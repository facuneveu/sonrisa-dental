namespace ClinicaDental.Services
{
    public class GeminiOptions
    {
        public const string Seccion = "Gemini";

        public string? ApiKey { get; set; }

        public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/";

        public string? Modelo { get; set; }

        public string[] Modelos { get; set; } = [];

        public int MaxTokensRespuesta { get; set; } = 2048;

        public int TimeoutSegundos { get; set; } = 30;

        public int TimeoutPorModeloSegundos { get; set; } = 10;

        public bool Configurado => !string.IsNullOrWhiteSpace(ApiKey);

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
