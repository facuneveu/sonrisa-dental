using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace ClinicaDental.Models
{
    // Cuenta de acceso (Identity). Los datos propios de cada rol viven en Paciente / Odontologo.
    public class Usuario : IdentityUser
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(50, ErrorMessage = "El nombre no puede superar los {1} caracteres.")]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio.")]
        [StringLength(50, ErrorMessage = "El apellido no puede superar los {1} caracteres.")]
        [Display(Name = "Apellido")]
        public string Apellido { get; set; } = string.Empty;

        [Required(ErrorMessage = "El DNI es obligatorio.")]
        [Range(1000000, 99999999, ErrorMessage = "Ingresá un DNI válido (7 u 8 dígitos).")]
        [Display(Name = "DNI")]
        public int Dni { get; set; }

        [Display(Name = "Activo")]
        public bool Activo { get; set; } = true;

        [Display(Name = "Fecha de alta")]
        public DateTime FechaAlta { get; set; } = Services.Reloj.Ahora;

        [Display(Name = "Nombre completo")]
        public string NombreCompleto => $"{Apellido}, {Nombre}";
    }
}
