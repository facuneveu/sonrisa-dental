using System.ComponentModel.DataAnnotations;

namespace ClinicaDental.Models.ViewModels
{
    public class PerfilFormViewModel
    {
        [Phone(ErrorMessage = "Ingresá un teléfono válido.")]
        [StringLength(30, ErrorMessage = "El teléfono no puede superar los {1} caracteres.")]
        [Display(Name = "Teléfono")]
        public string? Telefono { get; set; }

        [StringLength(150, ErrorMessage = "La dirección no puede superar los {1} caracteres.")]
        [Display(Name = "Dirección")]
        public string? Direccion { get; set; }

        [Display(Name = "Obra social")]
        public int? ObraSocialId { get; set; }

        [StringLength(30, ErrorMessage = "El número de afiliado no puede superar los {1} caracteres.")]
        [Display(Name = "Nº de afiliado")]
        public string? NumeroAfiliado { get; set; }

        [StringLength(300, ErrorMessage = "Las alergias no pueden superar los {1} caracteres.")]
        [Display(Name = "Alergias / condiciones relevantes")]
        public string? Alergias { get; set; }
    }

    public class CambiarClaveViewModel
    {
        [Required(ErrorMessage = "Ingresá tu contraseña actual.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña actual")]
        public string ClaveActual { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ingresá la contraseña nueva.")]
        [StringLength(100, ErrorMessage = "La contraseña nueva debe tener entre {2} y {1} caracteres.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña nueva")]
        public string ClaveNueva { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Repetir contraseña nueva")]
        [Compare(nameof(ClaveNueva), ErrorMessage = "La contraseña nueva y su repetición no coinciden.")]
        public string ConfirmarClave { get; set; } = string.Empty;
    }

    public class PerfilViewModel
    {
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Dni { get; set; }
        public string Rol { get; set; } = string.Empty;

        public bool EsPaciente { get; set; }
        public bool EsOdontologo { get; set; }

        public DateOnly? FechaNacimiento { get; set; }
        public string? ObraSocial { get; set; }
        public string? CoberturaObraSocial { get; set; }

        public string? Matricula { get; set; }
        public string? Especialidad { get; set; }
        public List<HorarioAtencion> Horarios { get; set; } = new();

        public PerfilFormViewModel Datos { get; set; } = new();
        public CambiarClaveViewModel Clave { get; set; } = new();
    }
}
