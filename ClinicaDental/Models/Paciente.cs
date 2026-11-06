using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ClinicaDental.Models
{
    public class Paciente : AuditableEntity
    {
        public int Id { get; set; }

        public string UsuarioId { get; set; } = string.Empty;
        [BindNever, ValidateNever] public Usuario? Usuario { get; set; }

        [Display(Name = "Fecha de nacimiento")]
        public DateOnly FechaNacimiento { get; set; }

        [Display(Name = "Obra social")]
        public int? ObraSocialId { get; set; }
        [BindNever, ValidateNever] public ObraSocial? ObraSocial { get; set; }

        [StringLength(30, ErrorMessage = "El número de afiliado no puede superar los {1} caracteres.")]
        [Display(Name = "Nº de afiliado")]
        public string? NumeroAfiliado { get; set; }

        [StringLength(150, ErrorMessage = "La dirección no puede superar los {1} caracteres.")]
        [Display(Name = "Dirección")]
        public string? Direccion { get; set; }

        [StringLength(300, ErrorMessage = "Las alergias no pueden superar los {1} caracteres.")]
        [Display(Name = "Alergias / condiciones relevantes")]
        public string? Alergias { get; set; }

        [BindNever, ValidateNever] public ICollection<Turno> Turnos { get; set; } = new List<Turno>();
        [BindNever, ValidateNever] public ICollection<PiezaDental> Piezas { get; set; } = new List<PiezaDental>();
        [BindNever, ValidateNever] public ICollection<Adjunto> Adjuntos { get; set; } = new List<Adjunto>();
    }
}
