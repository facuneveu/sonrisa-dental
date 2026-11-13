using ClinicaDental.Data;
using Microsoft.EntityFrameworkCore;

namespace ClinicaDental.Services
{
    public class MensajesService
    {
        private readonly ApplicationDbContext _context;
        private readonly PerfilService _perfil;
        private int? _noLeidos;

        public MensajesService(ApplicationDbContext context, PerfilService perfil)
        {
            _context = context;
            _perfil = perfil;
        }

        public async Task<int> NoLeidosAsync()
        {
            if (_noLeidos is { } cacheado) return cacheado;

            var pacienteId = await _perfil.ObtenerPacienteIdAsync();
            _noLeidos = pacienteId == null
                ? 0
                : await _context.Mensajes.CountAsync(m => m.PacienteId == pacienteId && m.FechaLectura == null);
            return _noLeidos.Value;
        }
    }
}
