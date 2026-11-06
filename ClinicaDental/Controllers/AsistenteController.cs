using ClinicaDental.Models;
using ClinicaDental.Models.ViewModels;
using ClinicaDental.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClinicaDental.Controllers
{
    // Endpoint del chat flotante. Solo para personas con sesión iniciada (cualquier rol): el asistente consume una cuota
    // limitada de la API de IA, y dejarlo abierto a cualquiera permitiría agotarla. Además de eso, cada persona tiene un
    // tope de consultas por minuto (política "asistente" en Program.cs).
    [Authorize]
    public class AsistenteController : Controller
    {
        private readonly AsistenteService _asistente;
        private readonly ILogger<AsistenteController> _log;

        public AsistenteController(AsistenteService asistente, ILogger<AsistenteController> log)
        {
            _asistente = asistente;
            _log = log;
        }

        // El JSON viaja con el token antifalsificación en la cabecera RequestVerificationToken.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("asistente")]
        public async Task<IActionResult> Preguntar([FromBody] PreguntaAsistenteViewModel vm, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                var motivo = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault(m => !string.IsNullOrWhiteSpace(m));
                return BadRequest(new { ok = false, texto = motivo ?? "No entendí tu consulta. Probá escribirla de nuevo." });
            }

            var rol = User.IsInRole(Roles.Administrador) ? Roles.Administrador
                    : User.IsInRole(Roles.Odontologo) ? Roles.Odontologo
                    : Roles.Paciente;

            var respuesta = await _asistente.PreguntarAsync(rol, vm.Mensajes.Select(m => new MensajeChat(m.Rol, m.Texto)), ct);

            // No se guarda el contenido de las conversaciones: solo queda constancia de que se usó.
            _log.LogInformation("Asistente: consulta de {Rol} respondida con ok={Ok} (modelo {Modelo}).", rol, respuesta.Ok, respuesta.Modelo ?? "-");

            return Json(new { ok = respuesta.Ok, texto = respuesta.Texto });
        }
    }
}
