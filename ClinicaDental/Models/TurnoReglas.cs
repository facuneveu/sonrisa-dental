namespace ClinicaDental.Models
{
    public static class TurnoReglas
    {
        public static IReadOnlyList<EstadoTurno> Siguientes(EstadoTurno actual) => actual switch
        {
            EstadoTurno.Pendiente => new[] { EstadoTurno.Confirmado, EstadoTurno.Atendido, EstadoTurno.Ausente, EstadoTurno.Cancelado },
            EstadoTurno.Confirmado => new[] { EstadoTurno.Atendido, EstadoTurno.Ausente, EstadoTurno.Cancelado },
            _ => Array.Empty<EstadoTurno>()
        };

        public static bool PuedeCambiar(EstadoTurno actual, EstadoTurno nuevo) => Siguientes(actual).Contains(nuevo);

        public static bool PacientePuedeCancelar(Turno turno, DateTime ahora) =>
            turno.FechaHora > ahora && PuedeCambiar(turno.Estado, EstadoTurno.Cancelado);

        public static string Verbo(EstadoTurno estado) => estado switch
        {
            EstadoTurno.Confirmado => "Confirmar",
            EstadoTurno.Atendido => "Marcar como atendido",
            EstadoTurno.Ausente => "Marcar como ausente",
            EstadoTurno.Cancelado => "Cancelar turno",
            _ => estado.ToString()
        };

        public static string Icono(EstadoTurno estado) => estado switch
        {
            EstadoTurno.Confirmado => "bi-check2-circle",
            EstadoTurno.Atendido => "bi-clipboard2-check",
            EstadoTurno.Ausente => "bi-person-x",
            EstadoTurno.Cancelado => "bi-x-circle",
            _ => "bi-circle"
        };
    }
}
