using System.ComponentModel.DataAnnotations;

namespace ClinicaDental.Models.ViewModels
{
    public class HorarioFormViewModel
    {
        public int OdontologoId { get; set; }

        [Display(Name = "Día")]
        public DayOfWeek DiaSemana { get; set; } = DayOfWeek.Monday;

        [Required(ErrorMessage = "Indicá la hora de inicio.")]
        [Display(Name = "Desde")]
        public TimeOnly? HoraDesde { get; set; }

        [Required(ErrorMessage = "Indicá la hora de fin.")]
        [Display(Name = "Hasta")]
        public TimeOnly? HoraHasta { get; set; }
    }
}
