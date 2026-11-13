using System.ComponentModel.DataAnnotations;

namespace ClinicaDental.Models.ViewModels
{
    public class TurnoFormViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Paciente")]
        public int PacienteId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Seleccioná un tratamiento.")]
        [Display(Name = "Tratamiento")]
        public int TratamientoId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Seleccioná un odontólogo.")]
        [Display(Name = "Odontólogo")]
        public int OdontologoId { get; set; }

        [Required(ErrorMessage = "Elegí una fecha.")]
        [Display(Name = "Fecha")]
        public DateOnly? Fecha { get; set; }

        [Required(ErrorMessage = "Elegí un horario.")]
        [Display(Name = "Horario")]
        public TimeOnly? Hora { get; set; }

        [StringLength(300, ErrorMessage = "El motivo no puede superar los {1} caracteres.")]
        [Display(Name = "Motivo de la consulta")]
        public string? Motivo { get; set; }

        [Display(Name = "Estado")]
        public EstadoTurno Estado { get; set; } = EstadoTurno.Pendiente;

        [StringLength(500, ErrorMessage = "Las observaciones no pueden superar los {1} caracteres.")]
        [Display(Name = "Observaciones")]
        public string? Observaciones { get; set; }
    }
}
