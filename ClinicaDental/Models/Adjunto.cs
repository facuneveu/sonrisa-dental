using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ClinicaDental.Models
{
    // Archivo del paciente: radiografías (imágenes) y presupuestos / estudios (PDF).
    public class Adjunto : AuditableEntity
    {
        public int Id { get; set; }

        public int PacienteId { get; set; }
        [BindNever, ValidateNever] public Paciente? Paciente { get; set; }

        public int? HistoriaClinicaId { get; set; }
        [BindNever, ValidateNever] public HistoriaClinica? HistoriaClinica { get; set; }

        public TipoAdjunto Tipo { get; set; } = TipoAdjunto.Radiografia;

        [Required, StringLength(200)]
        [Display(Name = "Archivo")]
        public string NombreOriginal { get; set; } = string.Empty;

        // Ruta relativa dentro de wwwroot/uploads, con nombre generado (nunca el que manda el usuario).
        [Required, StringLength(300)]
        public string RutaArchivo { get; set; } = string.Empty;

        [StringLength(100)]
        public string TipoContenido { get; set; } = string.Empty;

        public long TamanoBytes { get; set; }

        [StringLength(200, ErrorMessage = "La descripción no puede superar los {1} caracteres.")]
        [Display(Name = "Descripción")]
        public string? Descripcion { get; set; }
    }
}
