using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ClinicaDental.Models
{
    public class Tratamiento : AuditableEntity
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del tratamiento es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede superar los {1} caracteres.")]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "La descripción no puede superar los {1} caracteres.")]
        [Display(Name = "Descripción")]
        public string? Descripcion { get; set; }

        [Range(15, 240, ErrorMessage = "La duración debe estar entre {1} y {2} minutos.")]
        [Display(Name = "Duración (minutos)")]
        public int DuracionMinutos { get; set; } = 30;

        [Range(typeof(decimal), "0", "10000000", ErrorMessage = "Ingresá un precio válido.")]
        [Display(Name = "Precio")]
        public decimal Precio { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Seleccioná una especialidad.")]
        [Display(Name = "Especialidad")]
        public int EspecialidadId { get; set; }

        [BindNever, ValidateNever] public Especialidad? Especialidad { get; set; }

        [Display(Name = "Disponible")]
        public bool Activo { get; set; } = true;
    }
}
