using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ClinicaDental.Models
{
    // Base de las entidades auditadas: el DbContext completa estos campos en cada SaveChanges.
    // [BindNever] evita que un formulario (o un request armado a mano) pueda falsificarlos.
    public abstract class AuditableEntity
    {
        [BindNever]
        [Display(Name = "Fecha de creación")]
        public DateTime FechaCreacion { get; set; }

        [BindNever]
        [Display(Name = "Creado por")]
        public string? UsuarioCreacionId { get; set; }

        [BindNever]
        [Display(Name = "Fecha de modificación")]
        public DateTime? FechaModificacion { get; set; }

        [BindNever]
        [Display(Name = "Modificado por")]
        public string? UsuarioModificacionId { get; set; }
    }
}
