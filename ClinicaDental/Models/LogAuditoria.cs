using System.ComponentModel.DataAnnotations;

namespace ClinicaDental.Models
{
    public class LogAuditoria
    {
        public long Id { get; set; }

        [Display(Name = "Fecha")]
        public DateTime Fecha { get; set; }

        public string? UsuarioId { get; set; }

        [Required, StringLength(200)]
        [Display(Name = "Usuario")]
        public string UsuarioNombre { get; set; } = "Sistema";

        [Required, StringLength(20)]
        [Display(Name = "Acción")]
        public string Accion { get; set; } = string.Empty;

        [Required, StringLength(60)]
        [Display(Name = "Entidad")]
        public string Entidad { get; set; } = string.Empty;

        [StringLength(60)]
        [Display(Name = "Registro")]
        public string? EntidadId { get; set; }

        [StringLength(2000)]
        [Display(Name = "Detalle")]
        public string? Detalle { get; set; }
    }

    public static class AccionAuditoria
    {
        public const string Crear = "Crear";
        public const string Modificar = "Modificar";
        public const string Eliminar = "Eliminar";
    }
}
