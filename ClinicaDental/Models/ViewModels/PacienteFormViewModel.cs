using System.ComponentModel.DataAnnotations;

namespace ClinicaDental.Models.ViewModels
{
    public class PacienteFormViewModel
    {
        public int Id { get; set; }

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
        public int? Dni { get; set; }

        [Required(ErrorMessage = "La fecha de nacimiento es obligatoria.")]
        [Display(Name = "Fecha de nacimiento")]
        public DateOnly? FechaNacimiento { get; set; }

        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "Ingresá un correo electrónico válido.")]
        [StringLength(100, ErrorMessage = "El correo no puede superar los {1} caracteres.")]
        [Display(Name = "Correo electrónico")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono es obligatorio.")]
        [Phone(ErrorMessage = "Ingresá un teléfono válido.")]
        [StringLength(30, ErrorMessage = "El teléfono no puede superar los {1} caracteres.")]
        [Display(Name = "Teléfono")]
        public string? Telefono { get; set; }

        [Display(Name = "Obra social")]
        public int? ObraSocialId { get; set; }

        [StringLength(30, ErrorMessage = "El número de afiliado no puede superar los {1} caracteres.")]
        [Display(Name = "Nº de afiliado")]
        public string? NumeroAfiliado { get; set; }

        [StringLength(150, ErrorMessage = "La dirección no puede superar los {1} caracteres.")]
        [Display(Name = "Dirección")]
        public string? Direccion { get; set; }

        [StringLength(300, ErrorMessage = "Las alergias no pueden superar los {1} caracteres.")]
        [Display(Name = "Alergias / condiciones relevantes")]
        public string? Alergias { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Contraseña inicial")]
        public string? Password { get; set; }

        [Display(Name = "Cuenta activa")]
        public bool Activo { get; set; } = true;
    }
}
