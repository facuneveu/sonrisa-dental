using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ClinicaDental.Models
{
    public class HorarioAtencion : AuditableEntity
    {
        public int Id { get; set; }

        public int OdontologoId { get; set; }
        [BindNever, ValidateNever] public Odontologo? Odontologo { get; set; }

        [Display(Name = "Día")]
        public DayOfWeek DiaSemana { get; set; }

        [Display(Name = "Desde")]
        public TimeOnly HoraDesde { get; set; }

        [Display(Name = "Hasta")]
        public TimeOnly HoraHasta { get; set; }
    }
}
