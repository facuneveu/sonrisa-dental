namespace ClinicaDental.Models
{
    // Nombres de rol en un solo lugar para no repetir strings en [Authorize] y en las vistas.
    public static class Roles
    {
        public const string Administrador = "Administrador";
        public const string Odontologo = "Odontologo";
        public const string Paciente = "Paciente";

        public const string AdminYOdontologo = Administrador + "," + Odontologo;
        public const string OdontologoOPaciente = Odontologo + "," + Paciente;

        public static readonly string[] Todos = { Administrador, Odontologo, Paciente };
    }
}
