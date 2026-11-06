using ClinicaDental.Models;
using ClinicaDental.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClinicaDental.Data
{
    // Datos ficticios para poder probar y mostrar el sistema desde el primer arranque.
    // Se desactivan con "SeedDatosEjemplo": false en appsettings.
    public static class DatosEjemplo
    {
        public const string ClaveOdontologos = "Odonto123!";
        public const string ClavePacientes = "Paciente123!";

        public static async Task SembrarAsync(IServiceProvider services)
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            if (await context.Especialidades.AnyAsync()) return;

            var userManager = services.GetRequiredService<UserManager<Usuario>>();

            // ---------- Catálogos ----------
            var general = new Especialidad { Nombre = "Odontología general", Descripcion = "Consultas, limpiezas, arreglos y prevención." };
            var ortodoncia = new Especialidad { Nombre = "Ortodoncia", Descripcion = "Corrección de la posición de dientes y mandíbulas." };
            var endodoncia = new Especialidad { Nombre = "Endodoncia", Descripcion = "Tratamientos de conducto y dolor dental." };
            var odontopediatria = new Especialidad { Nombre = "Odontopediatría", Descripcion = "Salud bucal de niñas y niños." };
            var implantes = new Especialidad { Nombre = "Implantología", Descripcion = "Reposición de piezas dentales con implantes." };
            context.Especialidades.AddRange(general, ortodoncia, endodoncia, odontopediatria, implantes);

            var saludPlena = new ObraSocial { Nombre = "Salud Plena", Telefono = "0800-555-0101", SitioWeb = "www.saludplena.example", Cobertura = "Cubre consultas, limpiezas y obturaciones al 100%. Ortodoncia y endodoncia con autorización previa.", Activa = true };
            var mediCoop = new ObraSocial { Nombre = "MediCoop", Telefono = "0800-555-0102", Cobertura = "Cobertura del 70% en prestaciones generales. No cubre blanqueamiento.", Activa = true };
            var vidaSegura = new ObraSocial { Nombre = "Vida Segura", Telefono = "0800-555-0103", Cobertura = "Cubre odontopediatría y prevención. Resto de las prestaciones con reintegro.", Activa = true };
            var prevencionSur = new ObraSocial { Nombre = "Prevención Sur", Telefono = "0800-555-0104", Cobertura = "Convenio finalizado.", Activa = false };
            context.ObrasSociales.AddRange(saludPlena, mediCoop, vidaSegura, prevencionSur);
            await context.SaveChangesAsync();

            Tratamiento T(string nombre, string desc, int minutos, decimal precio, Especialidad e) =>
                new() { Nombre = nombre, Descripcion = desc, DuracionMinutos = minutos, Precio = precio, EspecialidadId = e.Id, Activo = true };

            var consulta = T("Consulta y diagnóstico", "Revisión completa y plan de tratamiento.", 30, 15000, general);
            var limpieza = T("Limpieza dental", "Eliminación de sarro y placa, con pulido.", 45, 25000, general);
            var obturacion = T("Obturación (empaste)", "Reparación de una pieza con caries.", 45, 35000, general);
            var extraccion = T("Extracción simple", "Extracción de una pieza sin complicaciones.", 30, 30000, general);
            var blanqueamiento = T("Blanqueamiento dental", "Aclarado del color de los dientes en consultorio.", 60, 90000, general);
            var controlOrto = T("Control de ortodoncia", "Ajuste mensual de aparatología.", 30, 28000, ortodoncia);
            var brackets = T("Colocación de brackets", "Colocación de aparatología fija.", 90, 250000, ortodoncia);
            var conducto = T("Tratamiento de conducto", "Tratamiento endodóntico de una pieza.", 90, 120000, endodoncia);
            var reconducto = T("Retratamiento de conducto", "Repetición de un tratamiento previo.", 90, 150000, endodoncia);
            var controlNinos = T("Control odontopediátrico", "Revisión y fluorización para niñas y niños.", 30, 18000, odontopediatria);
            var sellador = T("Sellador de fosas y fisuras", "Protección preventiva de molares.", 30, 20000, odontopediatria);
            var evalImplante = T("Evaluación para implantes", "Estudio del caso y planificación.", 45, 22000, implantes);
            var implante = T("Colocación de implante", "Cirugía de colocación de un implante.", 120, 450000, implantes);
            context.Tratamientos.AddRange(consulta, limpieza, obturacion, extraccion, blanqueamiento, controlOrto, brackets,
                conducto, reconducto, controlNinos, sellador, evalImplante, implante);

            // ---------- Odontólogos ----------
            async Task<Odontologo> Od(string email, string nombre, string apellido, int dni, string tel, string matricula, Especialidad esp, string presentacion)
            {
                var u = await CrearUsuarioAsync(userManager, email, nombre, apellido, dni, tel, Roles.Odontologo, ClaveOdontologos);
                var o = new Odontologo { UsuarioId = u.Id, Matricula = matricula, EspecialidadId = esp.Id, Presentacion = presentacion };
                context.Odontologos.Add(o);
                return o;
            }

            var laura = await Od("odontologo@clinica.com", "Laura", "Benítez", 27456123, "351-555-0201", "MP 10234", general, "Odontóloga general con 15 años de experiencia en prevención y rehabilitación.");
            var martin = await Od("martin.quiroga@clinica.com", "Martín", "Quiroga", 29111876, "351-555-0202", "MP 11877", ortodoncia, "Especialista en ortodoncia para adolescentes y adultos.");
            var carolina = await Od("carolina.ferreyra@clinica.com", "Carolina", "Ferreyra", 30222987, "351-555-0203", "MP 12450", endodoncia, "Endodoncista. Tratamientos de conducto con microscopio.");
            var sebastian = await Od("sebastian.molina@clinica.com", "Sebastián", "Molina", 31333098, "351-555-0204", "MP 13002", odontopediatria, "Atención odontológica para niñas y niños.");
            var valeria = await Od("valeria.acosta@clinica.com", "Valeria", "Acosta", 28444109, "351-555-0205", "MP 13911", implantes, "Cirugía e implantología oral.");
            await context.SaveChangesAsync();

            var horarios = new List<HorarioAtencion>();
            void H(Odontologo o, int h1, int h2, params DayOfWeek[] dias)
            {
                foreach (var d in dias)
                    horarios.Add(new HorarioAtencion { OdontologoId = o.Id, DiaSemana = d, HoraDesde = new TimeOnly(h1, 0), HoraHasta = new TimeOnly(h2, 0) });
            }
            H(laura, 9, 13, DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday);
            H(laura, 15, 19, DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday);
            H(martin, 10, 14, DayOfWeek.Tuesday, DayOfWeek.Thursday);
            H(martin, 16, 20, DayOfWeek.Tuesday, DayOfWeek.Thursday);
            H(carolina, 14, 19, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday);
            H(sebastian, 9, 13, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Saturday);
            H(valeria, 9, 13, DayOfWeek.Thursday, DayOfWeek.Saturday);
            context.HorariosAtencion.AddRange(horarios);

            // ---------- Pacientes ----------
            async Task<Paciente> Pa(string email, string nombre, string apellido, int dni, string tel, DateOnly nac, ObraSocial? os, string? afiliado, string? alergias)
            {
                var u = await CrearUsuarioAsync(userManager, email, nombre, apellido, dni, tel, Roles.Paciente, ClavePacientes);
                var p = new Paciente
                {
                    UsuarioId = u.Id, FechaNacimiento = nac, ObraSocialId = os?.Id, NumeroAfiliado = afiliado,
                    Direccion = "Córdoba, Argentina", Alergias = alergias
                };
                context.Pacientes.Add(p);
                return p;
            }

            var lucia = await Pa("paciente@clinica.com", "Lucía", "Fernández", 40111222, "351-555-0301", new DateOnly(1995, 4, 12), saludPlena, "SP-448120", "Alergia a la penicilina");
            var tomas = await Pa("tomas.gomez@correo.test", "Tomás", "Gómez", 42222333, "351-555-0302", new DateOnly(2008, 9, 3), mediCoop, "MC-90311", null);
            var camila = await Pa("camila.rojas@correo.test", "Camila", "Rojas", 43333444, "351-555-0303", new DateOnly(2016, 1, 25), vidaSegura, "VS-20745", null);
            var joaquin = await Pa("joaquin.peralta@correo.test", "Joaquín", "Peralta", 38444555, "351-555-0304", new DateOnly(1988, 11, 30), null, null, "Hipertensión controlada");
            var agustina = await Pa("agustina.silva@correo.test", "Agustina", "Silva", 36555666, "351-555-0305", new DateOnly(1991, 6, 18), saludPlena, "SP-771034", null);
            await context.SaveChangesAsync();

            // ---------- Turnos ----------
            // Se ubican en la primera franja disponible a partir de una fecha, para que siempre caigan en
            // días y horas en que el odontólogo realmente atiende (sin importar cuándo se siembre la base).
            DateTime? Hueco(Odontologo o, int diasDesdeHoy, int indice, int duracion)
            {
                var desde = Reloj.Hoy.AddDays(diasDesdeHoy);
                for (var i = 0; i < 14; i++)
                {
                    var dia = desde.AddDays(i);
                    var franja = horarios.Where(h => h.OdontologoId == o.Id && h.DiaSemana == dia.DayOfWeek)
                                         .OrderBy(h => h.HoraDesde).FirstOrDefault();
                    if (franja == null) continue;

                    var inicio = dia.ToDateTime(franja.HoraDesde).AddMinutes(indice * 60);
                    return inicio.AddMinutes(duracion) <= dia.ToDateTime(franja.HoraHasta) ? inicio : null;
                }
                return null;
            }

            Turno? Tu(Paciente p, Odontologo o, Tratamiento t, EstadoTurno estado, int diasDesdeHoy, int indice, string? motivo)
            {
                var inicio = Hueco(o, diasDesdeHoy, indice, t.DuracionMinutos);
                if (inicio == null) return null;

                var turno = new Turno
                {
                    PacienteId = p.Id, OdontologoId = o.Id, TratamientoId = t.Id,
                    FechaHora = inicio.Value, DuracionMinutos = t.DuracionMinutos, Estado = estado, Motivo = motivo
                };
                context.Turnos.Add(turno);
                return turno;
            }

            var t1 = Tu(lucia, laura, limpieza, EstadoTurno.Atendido, -20, 0, "Limpieza semestral");
            var t2 = Tu(lucia, carolina, conducto, EstadoTurno.Atendido, -12, 0, "Dolor persistente en molar inferior");
            Tu(tomas, martin, brackets, EstadoTurno.Atendido, -30, 0, "Inicio de ortodoncia");
            Tu(camila, laura, consulta, EstadoTurno.Ausente, -9, 1, "Primera consulta");
            Tu(joaquin, laura, obturacion, EstadoTurno.Cancelado, -6, 2, "Caries en premolar");

            Tu(agustina, laura, consulta, EstadoTurno.Confirmado, 0, 1, "Control general");
            var tFuturoLucia = Tu(lucia, laura, obturacion, EstadoTurno.Confirmado, 1, 0, "Caries en molar superior");
            Tu(tomas, martin, controlOrto, EstadoTurno.Pendiente, 1, 0, "Control mensual");
            Tu(camila, sebastian, controlNinos, EstadoTurno.Pendiente, 1, 0, "Control anual");
            Tu(joaquin, carolina, reconducto, EstadoTurno.Pendiente, 2, 0, "Molestias en tratamiento anterior");
            Tu(agustina, valeria, evalImplante, EstadoTurno.Pendiente, 3, 0, "Consulta por pieza ausente");
            await context.SaveChangesAsync();

            // ---------- Historias clínicas y odontograma de ejemplo ----------
            if (t1 != null)
                context.HistoriasClinicas.Add(new HistoriaClinica
                {
                    TurnoId = t1.Id,
                    Diagnostico = "Acumulación de sarro leve. Encías sanas.",
                    Procedimiento = "Limpieza con ultrasonido y pulido.",
                    Indicaciones = "Cepillado después de cada comida. Control en 6 meses."
                });
            if (t2 != null)
                context.HistoriasClinicas.Add(new HistoriaClinica
                {
                    TurnoId = t2.Id,
                    Diagnostico = "Pulpitis irreversible en pieza 36.",
                    Procedimiento = "Tratamiento de conducto en una sesión.",
                    Indicaciones = "Evitar masticar del lado tratado 48 hs. Colocar corona en 30 días."
                });

            await context.SaveChangesAsync();

            // ---------- Archivos y mensajes de ejemplo ----------
            var radiografia = ArchivosDeEjemplo.RadiografiaPng();
            var presupuesto = ArchivosDeEjemplo.PresupuestoPdf("Presupuesto de tratamiento", new[]
            {
                "Paciente: Fernandez, Lucia",
                "Odontologa: Ferreyra, Carolina",
                "",
                "1) Tratamiento de conducto, pieza 36 ........ $ 120.000",
                "2) Corona sobre pieza 36 ..................... $ 180.000",
                "",
                "Total estimado: $ 300.000",
                "Validez del presupuesto: 30 dias.",
                "Documento ficticio de ejemplo."
            });

            context.Adjuntos.AddRange(
                new Adjunto
                {
                    PacienteId = lucia.Id, HistoriaClinicaId = t2 == null ? null : (await context.HistoriasClinicas.FirstAsync(h => h.TurnoId == t2.Id)).Id,
                    Tipo = TipoAdjunto.Radiografia, NombreOriginal = "radiografia-pieza-36.png", TipoContenido = "image/png",
                    TamanoBytes = radiografia.Length, Contenido = radiografia, Descripcion = "Control previo al tratamiento de conducto"
                },
                new Adjunto
                {
                    PacienteId = lucia.Id, Tipo = TipoAdjunto.Presupuesto, NombreOriginal = "presupuesto-corona.pdf", TipoContenido = "application/pdf",
                    TamanoBytes = presupuesto.Length, Contenido = presupuesto, Descripcion = "Presupuesto para la corona de la pieza 36"
                });

            // Un aviso ya leído y uno sin leer (para ver el contador y el aviso en el inicio de la paciente de prueba).
            context.Mensajes.AddRange(
                new Mensaje
                {
                    OdontologoId = carolina.Id, PacienteId = lucia.Id, Tipo = TipoMensaje.Indicaciones,
                    Asunto = "Cuidados después del tratamiento de conducto",
                    Texto = "Hola Lucía, recordá evitar masticar del lado tratado durante 48 horas y tomar la medicación indicada. Cualquier molestia fuerte, avisame.",
                    FechaEnvio = Reloj.Ahora.AddDays(-12), FechaLectura = Reloj.Ahora.AddDays(-12).AddHours(2)
                },
                new Mensaje
                {
                    OdontologoId = laura.Id, PacienteId = lucia.Id, TurnoId = tFuturoLucia?.Id, Tipo = TipoMensaje.Demora,
                    Asunto = "Demora en tu próximo turno",
                    Texto = "Hola Lucía, te aviso que ese día voy a estar con una demora de unos 15 minutos. Si preferís, podés llegar un poco más tarde. Disculpá las molestias.",
                    FechaEnvio = Reloj.Ahora.AddHours(-3)
                });

            PiezaDental Pi(int pieza, EstadoPieza estado, string? obs = null) =>
                new() { PacienteId = lucia.Id, NumeroPieza = pieza, Estado = estado, Observaciones = obs };
            context.PiezasDentales.AddRange(
                Pi(16, EstadoPieza.Obturada), Pi(26, EstadoPieza.Caries, "A tratar en el próximo turno"),
                Pi(36, EstadoPieza.Endodoncia, "Corona pendiente"), Pi(18, EstadoPieza.Ausente), Pi(46, EstadoPieza.Corona));

            await context.SaveChangesAsync();
        }

        private static async Task<Usuario> CrearUsuarioAsync(UserManager<Usuario> userManager, string email, string nombre,
            string apellido, int dni, string telefono, string rol, string clave)
        {
            var existente = await userManager.FindByEmailAsync(email);
            if (existente != null) return existente;

            var usuario = new Usuario
            {
                UserName = email, Email = email, EmailConfirmed = true, PhoneNumber = telefono,
                Nombre = nombre, Apellido = apellido, Dni = dni, Activo = true
            };

            var resultado = await userManager.CreateAsync(usuario, clave);
            if (!resultado.Succeeded)
                throw new InvalidOperationException($"No se pudo crear {email}: {string.Join("; ", resultado.Errors.Select(e => e.Description))}");

            await userManager.AddToRoleAsync(usuario, rol);
            return usuario;
        }
    }
}
