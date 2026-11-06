using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ClinicaDental.Models
{
    public class Especialidad : AuditableEntity
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre de la especialidad es obligatorio.")]
        [StringLength(80, ErrorMessage = "El nombre no puede superar los {1} caracteres.")]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(300, ErrorMessage = "La descripción no puede superar los {1} caracteres.")]
        [Display(Name = "Descripción")]
        public string? Descripcion { get; set; }

        [BindNever, ValidateNever] public ICollection<Tratamiento> Tratamientos { get; set; } = new List<Tratamiento>();
        [BindNever, ValidateNever] public ICollection<Odontologo> Odontologos { get; set; } = new List<Odontologo>();
    }
}
