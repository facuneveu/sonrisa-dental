using ClinicaDental.Data;
using Microsoft.EntityFrameworkCore;

namespace ClinicaDental.Services
{
    public class NombresUsuarioService
    {
        private readonly ApplicationDbContext _context;
        private Dictionary<string, string>? _nombres;

        public NombresUsuarioService(ApplicationDbContext context) => _context = context;

        public async Task<string> NombreAsync(string? usuarioId)
        {
            if (string.IsNullOrEmpty(usuarioId)) return "Sistema";

            _nombres ??= await _context.Users.AsNoTracking()
                .Select(u => new { u.Id, u.Nombre, u.Apellido })
                .ToDictionaryAsync(u => u.Id, u => u.Nombre + " " + u.Apellido);

            return _nombres.TryGetValue(usuarioId, out var nombre) ? nombre : "Usuario eliminado";
        }
    }
}
