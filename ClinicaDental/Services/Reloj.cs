namespace ClinicaDental.Services
{
    // Hora de la clínica (Argentina). En el hosting el servidor suele estar en UTC, y sin esto
    // "ahora", los turnos del día y las fechas de auditoría saldrían corridos 3 horas.
    public static class Reloj
    {
        private static readonly TimeZoneInfo Zona = ObtenerZona();

        public static DateTime Ahora => TimeZoneInfo.ConvertTime(DateTime.UtcNow, Zona);

        public static DateOnly Hoy => DateOnly.FromDateTime(Ahora);

        private static TimeZoneInfo ObtenerZona()
        {
            foreach (var id in new[] { "America/Argentina/Buenos_Aires", "Argentina Standard Time" })
            {
                try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
                catch (TimeZoneNotFoundException) { }
                catch (InvalidTimeZoneException) { }
            }
            return TimeZoneInfo.Local;
        }
    }
}
