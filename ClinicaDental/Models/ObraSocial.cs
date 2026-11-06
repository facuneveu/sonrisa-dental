using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ClinicaDental.Models
{
    public class ObraSocial : AuditableEntity
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre de la obra social es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede superar los {1} caracteres.")]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(30, ErrorMessage = "El teléfono no puede superar los {1} caracteres.")]
        [Display(Name = "Teléfono")]
        public string? Telefono { get; set; }

        [StringLength(150, ErrorMessage = "El sitio web no puede superar los {1} caracteres.")]
        [Display(Name = "Sitio web")]
        public string? SitioWeb { get; set; }

        [StringLength(500, ErrorMessage = "La cobertura no puede superar los {1} caracteres.")]
        [Display(Name = "Cobertura odontológica")]
        public string? Cobertura { get; set; }

        [Display(Name = "Convenio vigente")]
        public bool Activa { get; set; } = true;

        [BindNever, ValidateNever] public ICollection<Paciente> Pacientes { get; set; } = new List<Paciente>();
    }
}
