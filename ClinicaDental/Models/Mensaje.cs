using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ClinicaDental.Models
{
    [ReservadoEnAuditoria]
    public class Mensaje : AuditableEntity
    {
        public int Id { get; set; }

        public int OdontologoId { get; set; }
        [BindNever, ValidateNever] public Odontologo? Odontologo { get; set; }

        public int PacienteId { get; set; }
        [BindNever, ValidateNever] public Paciente? Paciente { get; set; }

        public int? TurnoId { get; set; }
        [BindNever, ValidateNever] public Turno? Turno { get; set; }

        public TipoMensaje Tipo { get; set; } = TipoMensaje.Otro;

        [Required, StringLength(100)]
        public string Asunto { get; set; } = string.Empty;

        [Required, StringLength(1000)]
        public string Texto { get; set; } = string.Empty;

        public DateTime FechaEnvio { get; set; }

        public DateTime? FechaLectura { get; set; }

        public bool Leido => FechaLectura != null;
    }
}
