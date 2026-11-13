using System.ComponentModel.DataAnnotations;

namespace ClinicaDental.Models.ViewModels
{
    public class OdontologoFormViewModel
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

        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "Ingresá un correo electrónico válido.")]
        [StringLength(100, ErrorMessage = "El correo no puede superar los {1} caracteres.")]
        [Display(Name = "Correo electrónico")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Ingresá un teléfono válido.")]
        [StringLength(30, ErrorMessage = "El teléfono no puede superar los {1} caracteres.")]
        [Display(Name = "Teléfono")]
        public string? Telefono { get; set; }

        [Required(ErrorMessage = "La matrícula es obligatoria.")]
        [StringLength(30, ErrorMessage = "La matrícula no puede superar los {1} caracteres.")]
        [Display(Name = "Matrícula")]
        public string Matricula { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Seleccioná una especialidad.")]
        [Display(Name = "Especialidad")]
        public int EspecialidadId { get; set; }

        [StringLength(500, ErrorMessage = "La presentación no puede superar los {1} caracteres.")]
        [Display(Name = "Presentación")]
        public string? Presentacion { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Contraseña inicial")]
        public string? Password { get; set; }

        [Display(Name = "Cuenta activa")]
        public bool Activo { get; set; } = true;
    }
}
