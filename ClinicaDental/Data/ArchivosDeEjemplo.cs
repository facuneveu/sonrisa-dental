using System.IO.Compression;
using System.Text;

namespace ClinicaDental.Data
{
    public static class ArchivosDeEjemplo
    {
        public static byte[] RadiografiaPng()
        {
            const int ancho = 260, alto = 150;
            var filas = new byte[(ancho + 1) * alto];

            for (var y = 0; y < alto; y++)
            {
                filas[y * (ancho + 1)] = 0;
                for (var x = 0; x < ancho; x++)
                {
                    double brillo = 22 + 14 * Math.Sin(x / 23.0) * Math.Cos(y / 17.0);

                    for (var i = 0; i < 10; i++)
                    {
                        double cx = 22 + i * 24;
                        double cy = 74 + 9 * Math.Cos((i - 4.5) / 4.5 * Math.PI / 2);
                        brillo = Math.Max(brillo, Elipse(x, y, cx, cy - 6, 9.5, 15, 215));
                        brillo = Math.Max(brillo, Elipse(x, y, cx, cy + 20, 3.6, 19, 165));
                    }
                    brillo = Math.Max(brillo, Elipse(x, y, 130, 138, 118, 9, 90));

                    filas[y * (ancho + 1) + 1 + x] = (byte)Math.Clamp((int)brillo, 0, 255);
                }
            }

            return CodificarPng(ancho, alto, filas);
        }

        private static double Elipse(double x, double y, double cx, double cy, double rx, double ry, double valor)
        {
            var d = Math.Pow((x - cx) / rx, 2) + Math.Pow((y - cy) / ry, 2);
            return d <= 1 ? valor * (1 - 0.35 * d) : 0;
        }

        private static byte[] CodificarPng(int ancho, int alto, byte[] filas)
        {
            using var salida = new MemoryStream();
            salida.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

            var ihdr = new byte[13];
            EscribirEntero(ihdr, 0, ancho);
            EscribirEntero(ihdr, 4, alto);
            ihdr[8] = 8;
            ihdr[9] = 0;
            Chunk(salida, "IHDR", ihdr);

            using (var comprimido = new MemoryStream())
            {
                using (var z = new ZLibStream(comprimido, CompressionLevel.Optimal, leaveOpen: true)) z.Write(filas);
                Chunk(salida, "IDAT", comprimido.ToArray());
            }

            Chunk(salida, "IEND", Array.Empty<byte>());
            return salida.ToArray();
        }

        private static void Chunk(Stream salida, string tipo, byte[] datos)
        {
            var longitud = new byte[4];
            EscribirEntero(longitud, 0, datos.Length);
            salida.Write(longitud);

            var tipoYDatos = new byte[4 + datos.Length];
            Encoding.ASCII.GetBytes(tipo, 0, 4, tipoYDatos, 0);
            datos.CopyTo(tipoYDatos, 4);
            salida.Write(tipoYDatos);

            var crc = new byte[4];
            EscribirEntero(crc, 0, unchecked((int)Crc32(tipoYDatos)));
            salida.Write(crc);
        }

        private static void EscribirEntero(byte[] destino, int posicion, int valor)
        {
            destino[posicion] = (byte)(valor >> 24);
            destino[posicion + 1] = (byte)(valor >> 16);
            destino[posicion + 2] = (byte)(valor >> 8);
            destino[posicion + 3] = (byte)valor;
        }

        private static uint Crc32(byte[] datos)
        {
            var crc = 0xFFFFFFFFu;
            foreach (var b in datos)
            {
                crc ^= b;
                for (var k = 0; k < 8; k++) crc = (crc & 1) != 0 ? 0xEDB88320u ^ (crc >> 1) : crc >> 1;
            }
            return ~crc;
        }

        public static byte[] PresupuestoPdf(string titulo, string[] lineas)
        {
            var contenido = new StringBuilder("BT /F1 20 Tf 60 780 Td (" + Escapar(titulo) + ") Tj ET\n");
            var y = 740;
            foreach (var linea in lineas)
            {
                contenido.Append($"BT /F1 12 Tf 60 {y} Td ({Escapar(linea)}) Tj ET\n");
                y -= 22;
            }

            var pdf = new StringBuilder("%PDF-1.4\n");
            var posiciones = new List<int>();
            void Objeto(string cuerpo)
            {
                posiciones.Add(pdf.Length);
                pdf.Append(posiciones.Count).Append(" 0 obj\n").Append(cuerpo).Append("\nendobj\n");
            }

            Objeto("<< /Type /Catalog /Pages 2 0 R >>");
            Objeto("<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
            Objeto("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>");
            Objeto($"<< /Length {contenido.Length} >>\nstream\n{contenido}endstream");
            Objeto("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");

            var inicioXref = pdf.Length;
            pdf.Append($"xref\n0 {posiciones.Count + 1}\n0000000000 65535 f \n");
            foreach (var p in posiciones) pdf.Append($"{p:D10} 00000 n \n");
            pdf.Append($"trailer\n<< /Size {posiciones.Count + 1} /Root 1 0 R >>\nstartxref\n{inicioXref}\n%%EOF");

            return Encoding.ASCII.GetBytes(pdf.ToString());
        }

        private static string Escapar(string texto) => texto.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    }
}
