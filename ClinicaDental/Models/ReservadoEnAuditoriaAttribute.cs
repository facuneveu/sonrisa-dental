namespace ClinicaDental.Models
{
    // Marca las entidades cuyo contenido es privado (historia clínica, mensajes, archivos del paciente).
    // La auditoría registra que se creó / cambió / eliminó el registro y quién lo hizo, pero NO copia el contenido
    // al registro de auditoría, que puede leer la administración.
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ReservadoEnAuditoriaAttribute : Attribute { }
}
