using System.ComponentModel.DataAnnotations;

namespace ClinicaDental.Models
{
    public enum EstadoTurno
    {
        Pendiente = 0,
        Confirmado = 1,
        Atendido = 2,
        Cancelado = 3,
        Ausente = 4
    }

    public enum EstadoPieza
    {
        Sana = 0,
        Caries = 1,
        Obturada = 2,
        Endodoncia = 3,
        Corona = 4,
        Implante = 5,
        Ausente = 6,
        [Display(Name = "Extracción indicada")] ExtraccionIndicada = 7
    }

    public enum TipoAdjunto
    {
        Radiografia = 0,
        Presupuesto = 1,
        Estudio = 2,
        Otro = 3
    }
}

namespace ClinicaDental.Models
{
    public enum TipoMensaje
    {
        Demora = 0,
        [Display(Name = "Reprogramación")] Reprogramacion = 1,
        [Display(Name = "Cancelación")] Cancelacion = 2,
        Indicaciones = 3,
        Otro = 4
    }
}
