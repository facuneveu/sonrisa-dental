using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace ClinicaDental.Services
{
    // Un mensaje de la conversación. Rol: "user" (la persona) o "model" (el asistente), como lo pide la API.
    public record MensajeChat(string Rol, string Texto);

    // Resultado de una consulta. Cuando Ok es false, Texto es un mensaje en español listo para mostrarle a la persona
    // (el detalle técnico queda en el registro del servidor, nunca en pantalla).
    public record RespuestaIA(bool Ok, string Texto, string? Modelo = null)
    {
        public static RespuestaIA Error(string mensaje) => new(false, mensaje);
    }

    // Cliente de la API REST de Google Gemini con HttpClient (sin paquetes extra).
    //   POST {BaseUrl}v1beta/models/{modelo}:generateContent
    //   La clave viaja en la cabecera x-goog-api-key y no en la URL, para que no aparezca en los registros.
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

            HttpStatusCode? ultimoEstado = null;

            foreach (var modelo in _opciones.ModelosEnOrden())
            {
                using var pedido = new HttpRequestMessage(HttpMethod.Post, $"v1beta/models/{Uri.EscapeDataString(modelo)}:generateContent")
                {
                    Content = new StringContent(cuerpo, Encoding.UTF8, "application/json")
                };
                pedido.Headers.Add("x-goog-api-key", _opciones.ApiKey);

                HttpResponseMessage respuesta;
                try
                {
                    respuesta = await _http.SendAsync(pedido, ct);
                }
                catch (TaskCanceledException) when (!ct.IsCancellationRequested)
                {
                    _log.LogWarning("Gemini ({Modelo}) no respondió en {Segundos} s.", modelo, _opciones.TimeoutSegundos);
                    return RespuestaIA.Error("El asistente está tardando demasiado en responder. Probá de nuevo en un momento.");
                }
                catch (HttpRequestException ex)
                {
                    _log.LogWarning(ex, "No se pudo conectar con Gemini ({Modelo}).", modelo);
                    return RespuestaIA.Error("No pude conectarme con el asistente. Probá de nuevo en un momento.");
                }

                using (respuesta)
                {
                    var texto = await respuesta.Content.ReadAsStringAsync(ct);
                    ultimoEstado = respuesta.StatusCode;

                    if (respuesta.IsSuccessStatusCode)
                    {
                        var resultado = Interpretar(texto, modelo);
                        if (resultado != null) return resultado;
                        continue; // respuesta vacía: se prueba con el modelo siguiente
                    }

                    _log.LogWarning("Gemini ({Modelo}) respondió {Estado}: {Detalle}", modelo, (int)respuesta.StatusCode, Recortar(texto, 300));

                    // La clave mala no mejora cambiando de modelo.
                    if (respuesta.StatusCode == HttpStatusCode.Unauthorized
                        || (respuesta.StatusCode == HttpStatusCode.BadRequest && texto.Contains("API_KEY_INVALID", StringComparison.Ordinal)))
                        return RespuestaIA.Error("La clave del asistente no es válida. Avisale a la administración de la clínica.");

                    // 400 / 403 / 404 / 429 / 5xx: el modelo no existe, no está disponible para esta clave o no tiene cuota.
                }
            }

            return ultimoEstado == HttpStatusCode.TooManyRequests
                ? RespuestaIA.Error("El asistente recibió muchas consultas seguidas. Probá de nuevo en un minuto.")
                : RespuestaIA.Error("El asistente no está disponible en este momento. Probá de nuevo más tarde.");
        }

        // Devuelve null si la respuesta no trae texto utilizable (para probar con otro modelo).
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
                        // Los modelos con razonamiento pueden devolver su "pensamiento" marcado aparte: no se muestra.
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

        private static string Recortar(string texto, int max) =>
            texto.Length <= max ? texto : texto[..max] + "…";
    }
}
