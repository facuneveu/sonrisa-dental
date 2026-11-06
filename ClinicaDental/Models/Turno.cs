using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ClinicaDental.Models
{
    public class Turno : AuditableEntity
    {
        public int Id { get; set; }

        public int PacienteId { get; set; }
        [BindNever, ValidateNever] public Paciente? Paciente { get; set; }

        public int OdontologoId { get; set; }
        [BindNever, ValidateNever] public Odontologo? Odontologo { get; set; }

        public int TratamientoId { get; set; }
        [BindNever, ValidateNever] public Tratamiento? Tratamiento { get; set; }

        [Display(Name = "Fecha y hora")]
        public DateTime FechaHora { get; set; }

        // Se copia del tratamiento al reservar: si después cambia la duración del tratamiento,
        // los turnos ya dados no se corren ni se superponen.
        [Display(Name = "Duración (min)")]
        public int DuracionMinutos { get; set; }

        public EstadoTurno Estado { get; set; } = EstadoTurno.Pendiente;

        [StringLength(300, ErrorMessage = "El motivo no puede superar los {1} caracteres.")]
        [Display(Name = "Motivo de la consulta")]
        public string? Motivo { get; set; }

        [StringLength(500, ErrorMessage = "Las observaciones no pueden superar los {1} caracteres.")]
        [Display(Name = "Observaciones")]
        public string? Observaciones { get; set; }

        [BindNever, ValidateNever] public HistoriaClinica? HistoriaClinica { get; set; }

        public DateTime FechaHoraFin => FechaHora.AddMinutes(DuracionMinutos);

        // Un turno "ocupa" el horario mientras no esté cancelado.
        public bool OcupaHorario => Estado != EstadoTurno.Cancelado;
    }
}
