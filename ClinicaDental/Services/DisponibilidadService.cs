using ClinicaDental.Data;
using ClinicaDental.Models;
using Microsoft.EntityFrameworkCore;

namespace ClinicaDental.Services
{
    // Reglas de agenda: qué horarios están libres y si un turno puede darse.
    // Vive en un servicio (y no en el controlador) porque lo usan tanto el selector de horarios
    // como la validación al guardar: así las dos cosas nunca pueden contradecirse.
    public class DisponibilidadService
    {
        public const int PasoMinutos = 15;

        private readonly ApplicationDbContext _context;

        public DisponibilidadService(ApplicationDbContext context) => _context = context;

        public async Task<List<TimeOnly>> HorariosLibresAsync(int odontologoId, DateOnly fecha, int duracionMin, int? ignorarTurnoId = null)
        {
            var libres = new List<TimeOnly>();

            var franjas = await FranjasDelDiaAsync(odontologoId, fecha);
            if (franjas.Count == 0) return libres;

            var ocupados = await OcupadosAsync(odontologoId, fecha, ignorarTurnoId);
            var ahora = Reloj.Ahora;

            foreach (var franja in franjas)
            {
                var desde = franja.HoraDesde.Hour * 60 + franja.HoraDesde.Minute;
                var hasta = franja.HoraHasta.Hour * 60 + franja.HoraHasta.Minute;

                // Se trabaja con minutos enteros (y no con TimeOnly.AddMinutes) porque TimeOnly
                // "da la vuelta" pasada la medianoche y el bucle nunca terminaría.
                for (var m = desde; m + duracionMin <= hasta; m += PasoMinutos)
                {
                    var inicio = fecha.ToDateTime(new TimeOnly(m / 60, m % 60));
                    var fin = inicio.AddMinutes(duracionMin);

                    if (inicio <= ahora) continue;
                    if (ocupados.Any(o => inicio < o.Fin && fin > o.Inicio)) continue;

                    libres.Add(TimeOnly.FromDateTime(inicio));
                }
            }

            return libres.Distinct().OrderBy(h => h).ToList();
        }

        // Devuelve el motivo por el que NO se puede dar el turno, o null si se puede.
        public async Task<string?> ValidarAsync(int odontologoId, DateTime inicio, int duracionMin, int? ignorarTurnoId = null)
        {
            var fin = inicio.AddMinutes(duracionMin);
            var fecha = DateOnly.FromDateTime(inicio);

            var franjas = await FranjasDelDiaAsync(odontologoId, fecha);
            var entraEnUnaFranja = franjas.Any(f =>
                TimeOnly.FromDateTime(inicio) >= f.HoraDesde &&
                fin.Date == inicio.Date && TimeOnly.FromDateTime(fin) <= f.HoraHasta);

            if (!entraEnUnaFranja)
                return "El odontólogo no atiende en ese día y horario.";

            var ocupados = await OcupadosAsync(odontologoId, fecha, ignorarTurnoId);
            if (ocupados.Any(o => inicio < o.Fin && fin > o.Inicio))
                return "Ese horario ya fue reservado. Elegí otro.";

            return null;
        }

        // ¿El paciente ya tiene otro turno (con cualquier odontólogo) que se pise con este?
        public async Task<bool> PacienteOcupadoAsync(int pacienteId, DateTime inicio, int duracionMin, int? ignorarTurnoId = null)
        {
            var fin = inicio.AddMinutes(duracionMin);
            var dia = inicio.Date;

            var turnos = await _context.Turnos.AsNoTracking()
                .Where(t => t.PacienteId == pacienteId
                            && t.FechaHora >= dia && t.FechaHora < dia.AddDays(1)
                            && t.Estado != EstadoTurno.Cancelado
                            && (ignorarTurnoId == null || t.Id != ignorarTurnoId))
                .Select(t => new { t.FechaHora, t.DuracionMinutos })
                .ToListAsync();

            return turnos.Any(t => inicio < t.FechaHora.AddMinutes(t.DuracionMinutos) && fin > t.FechaHora);
        }

        private Task<List<HorarioAtencion>> FranjasDelDiaAsync(int odontologoId, DateOnly fecha) =>
            _context.HorariosAtencion.AsNoTracking()
                .Where(h => h.OdontologoId == odontologoId && h.DiaSemana == fecha.DayOfWeek)
                .OrderBy(h => h.HoraDesde)
                .ToListAsync();

        private async Task<List<(DateTime Inicio, DateTime Fin)>> OcupadosAsync(int odontologoId, DateOnly fecha, int? ignorarTurnoId)
        {
            var dia = fecha.ToDateTime(TimeOnly.MinValue);

            var turnos = await _context.Turnos.AsNoTracking()
                .Where(t => t.OdontologoId == odontologoId
                            && t.FechaHora >= dia && t.FechaHora < dia.AddDays(1)
                            && t.Estado != EstadoTurno.Cancelado
                            && (ignorarTurnoId == null || t.Id != ignorarTurnoId))
                .Select(t => new { t.FechaHora, t.DuracionMinutos })
                .ToListAsync();

            return turnos.Select(t => (t.FechaHora, t.FechaHora.AddMinutes(t.DuracionMinutos))).ToList();
        }
    }
}
