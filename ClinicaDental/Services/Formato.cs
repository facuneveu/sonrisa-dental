using System.Globalization;

namespace ClinicaDental.Services
{
    // Formatos para mostrar. La app procesa formularios con cultura en-US (los <input type="number">
    // envían "1500.50"), así que los formatos argentinos se piden de forma explícita acá.
    public static class Formato
    {
        private static readonly CultureInfo Ar = ObtenerCultura();

        public static string Moneda(decimal valor) => valor.ToString("C0", Ar);

        public static string Fecha(DateTime fecha) => fecha.ToString("dd/MM/yyyy", Ar);

        public static string Fecha(DateOnly fecha) => fecha.ToString("dd/MM/yyyy", Ar);

        public static string FechaHora(DateTime fecha) => fecha.ToString("dd/MM/yyyy HH:mm", Ar);

        public static string DiaSemana(DayOfWeek dia) => Ar.TextInfo.ToTitleCase(Ar.DateTimeFormat.GetDayName(dia));

        public static string DiaYFecha(DateTime fecha) => $"{DiaSemana(fecha.DayOfWeek)} {Fecha(fecha)}";

        // Nombre para mostrar de un enum: usa [Display(Name = "...")] si lo tiene (por ejemplo "Extracción indicada").
        public static string NombreEnum(Enum valor)
        {
            var campo = valor.GetType().GetField(valor.ToString());
            var display = campo?.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.DisplayAttribute), false)
                .Cast<System.ComponentModel.DataAnnotations.DisplayAttribute>().FirstOrDefault();
            return display?.Name ?? valor.ToString();
        }

        // Nombre de una pieza dental a partir de su número FDI: 38 -> "Tercer molar (muela del juicio) inferior izquierdo".
        // El número es una identificación (cuadrante + posición), no una cantidad de piezas.
        private static readonly string[] PosicionesFdi =
        {
            "", "Incisivo central", "Incisivo lateral", "Canino", "Primer premolar", "Segundo premolar",
            "Primer molar", "Segundo molar", "Tercer molar (muela del juicio)"
        };
        private static readonly string[] CuadrantesFdi = { "", "superior derecho", "superior izquierdo", "inferior izquierdo", "inferior derecho" };

        public static string NombrePieza(int numero) =>
            Models.PiezaDental.EsNumeroFdiValido(numero) ? $"{PosicionesFdi[numero % 10]} {CuadrantesFdi[numero / 10]}" : $"Pieza {numero}";

        // Nombre del cuadrante FDI (1 a 4), para los rótulos del odontograma.
        public static string NombreCuadrante(int cuadrante) => CuadrantesFdi[cuadrante];

        public static string TamanoArchivo(long bytes) =>
            bytes >= 1024 * 1024 ? $"{bytes / 1024d / 1024d:0.0} MB" : $"{Math.Max(1, bytes / 1024)} KB";

        private static readonly string[] DiasCortos = { "Dom", "Lun", "Mar", "Mié", "Jue", "Vie", "Sáb" };

        public static string DiaCorto(DayOfWeek dia) => DiasCortos[(int)dia];

        // Lunes primero (DayOfWeek arranca en domingo, que no es como se lee una agenda).
        public static int OrdenDia(DayOfWeek dia) => ((int)dia + 6) % 7;

        // "Lun 09:00–13:00, 15:00–19:00 · Mié 09:00–13:00"
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
