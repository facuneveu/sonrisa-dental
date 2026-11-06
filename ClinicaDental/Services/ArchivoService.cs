using System.Text;

namespace ClinicaDental.Services
{
    public record ArchivoValidado(bool Ok, string? Error, byte[] Contenido, string TipoContenido, string NombreOriginal);

    // Valida los archivos que sube el odontólogo (radiografías y estudios). No confía en nada de lo que declara el
    // navegador: el tipo se decide por la extensión permitida Y se comprueba contra los primeros bytes del archivo,
    // así un ejecutable renombrado a ".png" no entra. El nombre original solo se guarda para mostrarlo.
    public class ArchivoService
    {
        public const long TamanoMaximoBytes = 5 * 1024 * 1024;

        private static readonly Dictionary<string, string> TiposPermitidos = new(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".webp"] = "image/webp",
            [".pdf"] = "application/pdf"
        };

        public static bool EsImagen(string tipoContenido) => tipoContenido.StartsWith("image/", StringComparison.Ordinal);

        public async Task<ArchivoValidado> LeerYValidarAsync(IFormFile? archivo)
        {
            if (archivo == null || archivo.Length == 0)
                return Error("Seleccioná un archivo para subir.");

            if (archivo.Length > TamanoMaximoBytes)
                return Error($"El archivo supera el máximo de {TamanoMaximoBytes / 1024 / 1024} MB.");

            var extension = Path.GetExtension(archivo.FileName);
            if (!TiposPermitidos.TryGetValue(extension, out var tipo))
                return Error("Formato no permitido. Subí una imagen (JPG, PNG o WEBP) o un PDF.");

            using var memoria = new MemoryStream((int)archivo.Length);
            await archivo.CopyToAsync(memoria);
            var bytes = memoria.ToArray();

            if (!FirmaCoincide(extension, bytes))
                return Error("El contenido del archivo no corresponde a su formato. Verificá que no esté dañado.");

            return new ArchivoValidado(true, null, bytes, tipo, NombreSeguro(archivo.FileName, extension));
        }

        private static ArchivoValidado Error(string mensaje) => new(false, mensaje, Array.Empty<byte>(), "", "");

        private static bool FirmaCoincide(string extension, byte[] b)
        {
            if (b.Length < 12) return false;
            return extension.ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF,
                ".png" => b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47 && b[4] == 0x0D && b[5] == 0x0A && b[6] == 0x1A && b[7] == 0x0A,
                ".webp" => Encoding.ASCII.GetString(b, 0, 4) == "RIFF" && Encoding.ASCII.GetString(b, 8, 4) == "WEBP",
                ".pdf" => Encoding.ASCII.GetString(b, 0, 5) == "%PDF-",
                _ => false
            };
        }

        // Nombre apto para mostrar y para el encabezado de descarga: sin rutas, sin caracteres de control ni inválidos.
        private static string NombreSeguro(string nombreEnviado, string extension)
        {
            var nombre = Path.GetFileNameWithoutExtension(Path.GetFileName(nombreEnviado));
            var invalidos = Path.GetInvalidFileNameChars();
            var limpio = new string(nombre.Where(c => !char.IsControl(c) && !invalidos.Contains(c)).ToArray()).Trim();
            if (limpio.Length == 0) limpio = "archivo";
            if (limpio.Length > 150) limpio = limpio[..150];
            return limpio + extension.ToLowerInvariant();
        }
    }
}
