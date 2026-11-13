using System.Globalization;

namespace ClinicaDental.Services
{
    public static class Formato
    {
        private static readonly CultureInfo Ar = ObtenerCultura();

        public static string Moneda(decimal valor) => valor.ToString("C0", Ar);

        public static string Fecha(DateTime fecha) => fecha.ToString("dd/MM/yyyy", Ar);

        public static string Fecha(DateOnly fecha) => fecha.ToString("dd/MM/yyyy", Ar);

        public static string FechaHora(DateTime fecha) => fecha.ToString("dd/MM/yyyy HH:mm", Ar);

        public static string DiaSemana(DayOfWeek dia) => Ar.TextInfo.ToTitleCase(Ar.DateTimeFormat.GetDayName(dia));

        public static string DiaYFecha(DateTime fecha) => $"{DiaSemana(fecha.DayOfWeek)} {Fecha(fecha)}";

        public static string NombreEnum(Enum valor)
        {
            var campo = valor.GetType().GetField(valor.ToString());
            var display = campo?.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.DisplayAttribute), false)
                .Cast<System.ComponentModel.DataAnnotations.DisplayAttribute>().FirstOrDefault();
            return display?.Name ?? valor.ToString();
        }

        private static readonly string[] PosicionesFdi =
        {
            "", "Incisivo central", "Incisivo lateral", "Canino", "Primer premolar", "Segundo premolar",
            "Primer molar", "Segundo molar", "Tercer molar (muela del juicio)"
        };
        private static readonly string[] CuadrantesFdi = { "", "superior derecho", "superior izquierdo", "inferior izquierdo", "inferior derecho" };

        public static string NombrePieza(int numero) =>
            Models.PiezaDental.EsNumeroFdiValido(numero) ? $"{PosicionesFdi[numero % 10]} {CuadrantesFdi[numero / 10]}" : $"Pieza {numero}";

        public static string NombreCuadrante(int cuadrante) => CuadrantesFdi[cuadrante];

        public static string TamanoArchivo(long bytes) =>
            bytes >= 1024 * 1024 ? $"{bytes / 1024d / 1024d:0.0} MB" : $"{Math.Max(1, bytes / 1024)} KB";

        private static readonly string[] DiasCortos = { "Dom", "Lun", "Mar", "Mié", "Jue", "Vie", "Sáb" };

        public static string DiaCorto(DayOfWeek dia) => DiasCortos[(int)dia];

        public static int OrdenDia(DayOfWeek dia) => ((int)dia + 6) % 7;

        public static string ResumenHorarios(IEnumerable<Models.HorarioAtencion> horarios)
        {
            var partes = horarios
                .GroupBy(h => h.DiaSemana)
                .OrderBy(g => OrdenDia(g.Key))
                .Select(g => $"{DiaCorto(g.Key)} " + string.Join(", ",
                    g.OrderBy(h => h.HoraDesde).Select(h => $"{h.HoraDesde:HH:mm}–{h.HoraHasta:HH:mm}")));
            var texto = string.Join(" · ", partes);
            return texto.Length == 0 ? "Sin horarios cargados" : texto;
        }

        private static CultureInfo ObtenerCultura()
        {
            try { return CultureInfo.GetCultureInfo("es-AR"); }
            catch (CultureNotFoundException) { return CultureInfo.InvariantCulture; }
        }
    }
}
