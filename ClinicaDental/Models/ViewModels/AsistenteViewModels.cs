using System.ComponentModel.DataAnnotations;
using ClinicaDental.Services;

namespace ClinicaDental.Models.ViewModels
{
    // Lo que acepta el endpoint del asistente: solo la conversación. El rol de quien pregunta sale de la sesión,
    // no del request, para que nadie pueda pedir "como administrador".
    public class PreguntaAsistenteViewModel
    {
        [Required(ErrorMessage = "Escribí tu consulta.")]
        [MinLength(1, ErrorMessage = "Escribí tu consulta.")]
        [MaxLength(AsistenteService.MaxMensajes, ErrorMessage = "La conversación es demasiado larga. Borrala y empezá de nuevo.")]
        public List<MensajeAsistenteViewModel> Mensajes { get; set; } = new();
    }

    public class MensajeAsistenteViewModel
    {
        [Required]
        [RegularExpression("^(user|model)$", ErrorMessage = "Rol inválido.")]
        public string Rol { get; set; } = string.Empty;

        [Required(ErrorMessage = "Escribí tu consulta.")]
        [StringLength(AsistenteService.MaxCaracteresMensaje, ErrorMessage = "Cada mensaje puede tener hasta {1} caracteres.")]
        public string Texto { get; set; } = string.Empty;
    }
}
