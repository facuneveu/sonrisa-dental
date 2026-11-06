using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ClinicaDental.Models
{
    // Registro clínico de una consulta: una por turno atendido.
    [ReservadoEnAuditoria]
    public class HistoriaClinica : AuditableEntity
    {
        public int Id { get; set; }

        public int TurnoId { get; set; }
        [BindNever, ValidateNever] public Turno? Turno { get; set; }

        [Required(ErrorMessage = "El diagnóstico es obligatorio.")]
        [StringLength(1000, ErrorMessage = "El diagnóstico no puede superar los {1} caracteres.")]
        [Display(Name = "Diagnóstico")]
        public string Diagnostico { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "El procedimiento no puede superar los {1} caracteres.")]
        [Display(Name = "Procedimiento realizado")]
        public string? Procedimiento { get; set; }

        [StringLength(1000, ErrorMessage = "Las indicaciones no pueden superar los {1} caracteres.")]
        [Display(Name = "Indicaciones al paciente")]
        public string? Indicaciones { get; set; }

        [BindNever, ValidateNever] public ICollection<Adjunto> Adjuntos { get; set; } = new List<Adjunto>();
    }
}
