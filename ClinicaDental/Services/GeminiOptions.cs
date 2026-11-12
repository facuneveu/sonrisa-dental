namespace ClinicaDental.Services
{
    // Configuración del asistente. Se lee de la sección "Gemini" (appsettings.json, user-secrets o variables de entorno).
    //
    // La clave NUNCA va en un archivo del repositorio:
    //   desarrollo  ->  dotnet user-secrets set "Gemini:ApiKey" "TU-CLAVE" --project ClinicaDental
    //   producción  ->  variable de entorno  Gemini__ApiKey
    public class GeminiOptions
    {
        public const string Seccion = "Gemini";

        public string? ApiKey { get; set; }

        // Dirección base de la API. Es configurable para poder apuntar a un servidor simulado en las pruebas.
        public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/";

        // Si se define, es el primer modelo que se intenta.
        public string? Modelo { get; set; }

        // Modelos de respaldo, en orden. Google retira modelos con frecuencia (gemini-2.5-flash ya no se ofrece a claves
        // nuevas), así que si uno responde "no existe", "sin acceso" o "cuota agotada" se prueba el siguiente.
        public string[] Modelos { get; set; } = [];

        // Los modelos "con razonamiento" gastan parte de este tope pensando, por eso es holgado.
        public int MaxTokensRespuesta { get; set; } = 2048;

        // Tiempo máximo de toda la consulta, sumando los modelos que haga falta probar.
        public int TimeoutSegundos { get; set; } = 30;

        // Tiempo máximo que se le espera a cada modelo. Si no responde (o contesta 503 por demanda alta), se pasa al siguiente
        // en vez de dejar a la persona esperando: en las pruebas reales un modelo se colgaba los 30 segundos completos.
        public int TimeoutPorModeloSegundos { get; set; } = 10;

        public bool Configurado => !string.IsNullOrWhiteSpace(ApiKey);

        // El primero es el liviano y rápido (alcanza de sobra para preguntas frecuentes y orientación); los otros son respaldo.
        private static readonly string[] Predeterminados = ["gemini-3.5-flash-lite", "gemini-3.8-flash", "gemini-2.5-flash"];

        public IReadOnlyList<string> ModelosEnOrden()
        {
            var lista = new List<string>();
            if (!string.IsNullOrWhiteSpace(Modelo)) lista.Add(Modelo.Trim());
            lista.AddRange(Modelos.Length > 0 ? Modelos : Predeterminados);
            return lista.Where(m => !string.IsNullOrWhiteSpace(m)).Select(m => m.Trim()).Distinct().ToList();
        }
    }
}
