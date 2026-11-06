using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ClinicaDental.Models
{
    // Una fila del odontograma: estado de una pieza (numeración FDI) de un paciente.
    public class PiezaDental : AuditableEntity
    {
        public int Id { get; set; }

        public int PacienteId { get; set; }
        [BindNever, ValidateNever] public Paciente? Paciente { get; set; }

        [Range(11, 48, ErrorMessage = "La pieza debe estar entre 11 y 48 (notación FDI).")]
        [Display(Name = "Pieza")]
        public int NumeroPieza { get; set; }

        public EstadoPieza Estado { get; set; } = EstadoPieza.Sana;

        [StringLength(300, ErrorMessage = "Las observaciones no pueden superar los {1} caracteres.")]
        [Display(Name = "Observaciones")]
        public string? Observaciones { get; set; }
    }
}
