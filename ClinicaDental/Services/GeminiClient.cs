using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace ClinicaDental.Services
{
    public record MensajeChat(string Rol, string Texto);

    public record RespuestaIA(bool Ok, string Texto, string? Modelo = null)
    {
        public static RespuestaIA Error(string mensaje) => new(false, mensaje);
    }

    public class GeminiClient
    {
        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        private readonly HttpClient _http;
        private readonly GeminiOptions _opciones;
        private readonly ILogger<GeminiClient> _log;

        public GeminiClient(HttpClient http, Microsoft.Extensions.Options.IOptions<GeminiOptions> opciones, ILogger<GeminiClient> log)
        {
            _http = http;
            _opciones = opciones.Value;
            _log = log;
        }

        public bool Configurado => _opciones.Configurado;

        public async Task<RespuestaIA> GenerarAsync(string instrucciones, IReadOnlyList<MensajeChat> conversacion,
            CancellationToken ct = default)
        {
            if (!_opciones.Configurado)
                return RespuestaIA.Error("El asistente todavía no está configurado. Avisale a la administración de la clínica.");

            var cuerpo = JsonSerializer.Serialize(new
            {
                systemInstruction = new { parts = new[] { new { text = instrucciones } } },
                contents = conversacion.Select(m => new { role = m.Rol, parts = new[] { new { text = m.Texto } } }),
                generationConfig = new { maxOutputTokens = _opciones.MaxTokensRespuesta }
            }, Json);

            using var total = CancellationTokenSource.CreateLinkedTokenSource(ct);
            total.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(_opciones.TimeoutSegundos, 5, 120)));
            var porModelo = TimeSpan.FromSeconds(Math.Clamp(_opciones.TimeoutPorModeloSegundos, 2, 120));

            HttpStatusCode? ultimoEstado = null;
            var huboDemora = false;

            foreach (var modelo in _opciones.ModelosEnOrden())
            {
                for (var vez = 1; vez <= IntentosPorModelo; vez++)
                {
                    if (total.IsCancellationRequested) break;

                    using var intento = CancellationTokenSource.CreateLinkedTokenSource(total.Token);
                    intento.CancelAfter(porModelo);

                    using var pedido = new HttpRequestMessage(HttpMethod.Post, $"v1beta/models/{Uri.EscapeDataString(modelo)}:generateContent")
                    {
                        Content = new StringContent(cuerpo, Encoding.UTF8, "application/json")
                    };
                    pedido.Headers.Add("x-goog-api-key", _opciones.ApiKey);

                    try
                    {
                        using var respuesta = await _http.SendAsync(pedido, intento.Token);
                        var texto = await respuesta.Content.ReadAsStringAsync(intento.Token);
                        ultimoEstado = respuesta.StatusCode;

                        if (respuesta.IsSuccessStatusCode)
                        {
                            var resultado = Interpretar(texto, modelo);
                            if (resultado != null) return resultado;
                            break;
                        }

                        _log.LogWarning("Gemini ({Modelo}) respondió {Estado}: {Detalle}", modelo, (int)respuesta.StatusCode, Recortar(texto, 300));

                        if (respuesta.StatusCode == HttpStatusCode.Unauthorized
                            || (respuesta.StatusCode == HttpStatusCode.BadRequest && texto.Contains("API_KEY_INVALID", StringComparison.Ordinal)))
                            return RespuestaIA.Error("La clave del asistente no es válida. Avisale a la administración de la clínica.");

                        if (EsPasajero(respuesta.StatusCode) && vez < IntentosPorModelo)
                        {
                            await Task.Delay(400, total.Token);
                            continue;
                        }

                        break;
                    }
                    catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                    {
                        _log.LogWarning("Gemini ({Modelo}) no respondió a tiempo ({Segundos} s por modelo): se prueba con el siguiente.",
                            modelo, (int)porModelo.TotalSeconds);
                        huboDemora = true;
                        break;
                    }
                    catch (HttpRequestException ex)
                    {
                        _log.LogWarning(ex, "No se pudo conectar con Gemini ({Modelo}).", modelo);
                        return RespuestaIA.Error("No pude conectarme con el asistente. Probá de nuevo en un momento.");
                    }
                }
            }

            if (huboDemora)
                return RespuestaIA.Error("El asistente está tardando demasiado en responder. Probá de nuevo en un momento.");

            return ultimoEstado == HttpStatusCode.TooManyRequests
                ? RespuestaIA.Error("El asistente recibió muchas consultas seguidas. Probá de nuevo en un minuto.")
                : RespuestaIA.Error("El asistente no está disponible en este momento. Probá de nuevo más tarde.");
        }

        private RespuestaIA? Interpretar(string json, string modelo)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var raiz = doc.RootElement;

                if (raiz.TryGetProperty("promptFeedback", out var feedback) && feedback.TryGetProperty("blockReason", out _))
                    return RespuestaIA.Error("No puedo responder esa consulta. Probá reformulándola.");

                if (!raiz.TryGetProperty("candidates", out var candidatos) || candidatos.ValueKind != JsonValueKind.Array
                    || candidatos.GetArrayLength() == 0)
                    return null;

                var candidato = candidatos[0];
                var sb = new StringBuilder();
                if (candidato.TryGetProperty("content", out var contenido) && contenido.TryGetProperty("parts", out var partes))
                {
                    foreach (var parte in partes.EnumerateArray())
                    {
                        if (parte.TryGetProperty("thought", out var pensamiento) && pensamiento.ValueKind == JsonValueKind.True) continue;
                        if (parte.TryGetProperty("text", out var t) && t.ValueKind == JsonValueKind.String) sb.Append(t.GetString());
                    }
                }

                var respuesta = sb.ToString().Trim();
                if (respuesta.Length > 0) return new RespuestaIA(true, respuesta, modelo);

                if (candidato.TryGetProperty("finishReason", out var motivo) && motivo.GetString() is "SAFETY" or "PROHIBITED_CONTENT")
                    return RespuestaIA.Error("No puedo responder esa consulta. Probá reformulándola.");

                _log.LogWarning("Gemini ({Modelo}) devolvió una respuesta sin texto.", modelo);
                return null;
            }
            catch (JsonException ex)
            {
                _log.LogWarning(ex, "Gemini ({Modelo}) devolvió algo que no es JSON.", modelo);
                return null;
            }
        }

        private const int IntentosPorModelo = 2;

        private static bool EsPasajero(HttpStatusCode estado) =>
            estado is HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway
                or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout;

        private static string Recortar(string texto, int max) =>
            texto.Length <= max ? texto : texto[..max] + "…";
    }
}
