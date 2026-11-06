namespace ClinicaDental.Models.ViewModels
{
    public record Indicador(string Etiqueta, int Valor, string Icono, string? Url = null);

    public class InicioViewModel
    {
        public string Nombre { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public List<Indicador> Indicadores { get; set; } = new();
        public List<Turno> ProximosTurnos { get; set; } = new();
        public int MensajesSinLeer { get; set; }
    }
}
