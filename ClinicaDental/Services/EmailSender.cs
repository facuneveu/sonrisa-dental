using Microsoft.AspNetCore.Identity.UI.Services;

namespace ClinicaDental.Services
{
    // Identity pide un IEmailSender. En este proyecto no se envían correos reales
    // (la cuenta no requiere confirmación), así que no hace nada.
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage) => Task.CompletedTask;
    }
}
