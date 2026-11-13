using ClinicaDental.Models;
using ClinicaDental.Models.ViewModels;
using ClinicaDental.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClinicaDental.Controllers
{
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

            _log.LogInformation("Asistente: consulta de {Rol} respondida con ok={Ok} (modelo {Modelo}).", rol, respuesta.Ok, respuesta.Modelo ?? "-");

            return Json(new { ok = respuesta.Ok, texto = respuesta.Texto });
        }
    }
}
