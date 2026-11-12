using System.ComponentModel.DataAnnotations;

namespace ClinicaDental.Models.ViewModels
{
    // Una consulta ya registrada en la historia clínica.
    public record ConsultaItem(int Id, int TurnoId, DateTime Fecha, string Tratamiento, string Odontologo, bool EsMia,
        string Diagnostico, string? Procedimiento, string? Indicaciones, int Archivos);

    // Un turno atendido al que todavía no se le cargó la historia clínica.
    public record ConsultaPendiente(int TurnoId, DateTime Fecha, string Tratamiento);

    // Datos de un archivo SIN su contenido: las listas nunca cargan los bytes.
    public record AdjuntoResumen(int Id, TipoAdjunto Tipo, string Nombre, string TipoContenido, long TamanoBytes,
        string? Descripcion, int? HistoriaClinicaId, DateTime Fecha, string? SubidoPorId);

    // Fila de "Mis pacientes" del odontólogo.
    public record PacienteDelOdontologo(int Id, string Nombre, int Edad, string ObraSocial, string? Alergias,
        int Turnos, DateTime? UltimaConsulta, DateTime? ProximoTurno);

    public class FichaViewModel
    {
        public Paciente Paciente { get; set; } = null!;
        public int Edad { get; set; }

        // Solo el odontólogo modifica la ficha; el paciente la consulta.
        public bool PuedeEditar { get; set; }

        public List<ConsultaItem> Consultas { get; set; } = new();
        public List<ConsultaPendiente> Pendientes { get; set; } = new();
        public Dictionary<int, PiezaDental> Piezas { get; set; } = new();
        public List<AdjuntoResumen> Adjuntos { get; set; } = new();
    }

    public class PiezaFormViewModel
    {
        public int PacienteId { get; set; }

        [PiezaFdi]
        [Display(Name = "Pieza")]
        public int NumeroPieza { get; set; }

        [Display(Name = "Estado")]
        public EstadoPieza Estado { get; set; } = EstadoPieza.Sana;

        [StringLength(300, ErrorMessage = "Las observaciones no pueden superar los {1} caracteres.")]
        [Display(Name = "Observaciones")]
        public string? Observaciones { get; set; }
    }

    public class AdjuntoFormViewModel
    {
        public int PacienteId { get; set; }

        [Display(Name = "Consulta relacionada")]
        public int? HistoriaClinicaId { get; set; }

        [Display(Name = "Tipo")]
        public TipoAdjunto Tipo { get; set; } = TipoAdjunto.Radiografia;

        [StringLength(200, ErrorMessage = "La descripción no puede superar los {1} caracteres.")]
        [Display(Name = "Descripción")]
        public string? Descripcion { get; set; }

        public IFormFile? Archivo { get; set; }
    }

    public class MensajeFormViewModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "Elegí el paciente.")]
        [Display(Name = "Paciente")]
        public int PacienteId { get; set; }

        [Display(Name = "Turno relacionado")]
        public int? TurnoId { get; set; }

        [Display(Name = "Motivo")]
        public TipoMensaje Tipo { get; set; } = TipoMensaje.Demora;

        [Required(ErrorMessage = "Escribí el asunto del mensaje.")]
        [StringLength(100, ErrorMessage = "El asunto no puede superar los {1} caracteres.")]
        [Display(Name = "Asunto")]
        public string Asunto { get; set; } = string.Empty;

        [Required(ErrorMessage = "Escribí el mensaje.")]
        [StringLength(1000, ErrorMessage = "El mensaje no puede superar los {1} caracteres.")]
        [Display(Name = "Mensaje")]
        public string Texto { get; set; } = string.Empty;
    }
}
