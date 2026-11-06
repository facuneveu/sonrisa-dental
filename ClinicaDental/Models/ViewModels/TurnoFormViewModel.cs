using System.ComponentModel.DataAnnotations;

namespace ClinicaDental.Models.ViewModels
{
    // El turno se pide con campos que no existen tal cual en la tabla: "fecha" y "hora" por separado
    // (para elegir de los horarios libres) y un paciente que, si quien reserva es un paciente, se ignora
    // y se toma de su sesión. Por eso el formulario no se enlaza directo a la entidad Turno.
    public class TurnoFormViewModel
    {
        public int Id { get; set; }

        // Solo lo envía la administración. Para un paciente se ignora y se usa el de su cuenta.
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

        // Solo la administración puede editar estas dos.
        [Display(Name = "Estado")]
        public EstadoTurno Estado { get; set; } = EstadoTurno.Pendiente;

        [StringLength(500, ErrorMessage = "Las observaciones no pueden superar los {1} caracteres.")]
        [Display(Name = "Observaciones")]
        public string? Observaciones { get; set; }
    }
}
