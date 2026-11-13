using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ClinicaDental.Models
{
    public class Odontologo : AuditableEntity
    {
        public int Id { get; set; }

        public string UsuarioId { get; set; } = string.Empty;
        [BindNever, ValidateNever] public Usuario? Usuario { get; set; }

        [Required(ErrorMessage = "La matrícula es obligatoria.")]
        [StringLength(30, ErrorMessage = "La matrícula no puede superar los {1} caracteres.")]
        [Display(Name = "Matrícula")]
        public string Matricula { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Seleccioná una especialidad.")]
        [Display(Name = "Especialidad")]
        public int EspecialidadId { get; set; }
        [BindNever, ValidateNever] public Especialidad? Especialidad { get; set; }

        [StringLength(500, ErrorMessage = "La presentación no puede superar los {1} caracteres.")]
        [Display(Name = "Presentación")]
        public string? Presentacion { get; set; }

        [BindNever, ValidateNever] public ICollection<HorarioAtencion> Horarios { get; set; } = new List<HorarioAtencion>();
        [BindNever, ValidateNever] public ICollection<Turno> Turnos { get; set; } = new List<Turno>();
    }
}
