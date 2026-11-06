using System.Security.Claims;
using ClinicaDental.Data;
using Microsoft.EntityFrameworkCore;

namespace ClinicaDental.Services
{
    // Resuelve "¿qué odontólogo / paciente es el usuario logueado?". Se consulta una sola vez por request.
    public class PerfilService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _http;
        private bool _odontologoCargado, _pacienteCargado;
        private int? _odontologoId, _pacienteId;

        public PerfilService(ApplicationDbContext context, IHttpContextAccessor http)
        {
            _context = context;
            _http = http;
        }

        public string? UsuarioId => _http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

        public async Task<int?> ObtenerOdontologoIdAsync()
        {
            if (!_odontologoCargado)
            {
                var uid = UsuarioId;
                _odontologoId = uid == null ? null : await _context.Odontologos.AsNoTracking()
                    .Where(o => o.UsuarioId == uid).Select(o => (int?)o.Id).FirstOrDefaultAsync();
                _odontologoCargado = true;
            }
            return _odontologoId;
        }

        public async Task<int?> ObtenerPacienteIdAsync()
        {
            if (!_pacienteCargado)
            {
                var uid = UsuarioId;
                _pacienteId = uid == null ? null : await _context.Pacientes.AsNoTracking()
                    .Where(p => p.UsuarioId == uid).Select(p => (int?)p.Id).FirstOrDefaultAsync();
                _pacienteCargado = true;
            }
            return _pacienteId;
        }
    }
}
