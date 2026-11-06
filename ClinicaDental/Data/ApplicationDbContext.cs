using System.Security.Claims;
using ClinicaDental.Models;
using ClinicaDental.Services;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace ClinicaDental.Data
{
    public class ApplicationDbContext : IdentityDbContext<Usuario>
    {
        private readonly IHttpContextAccessor? _httpContextAccessor;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options,
            IHttpContextAccessor? httpContextAccessor = null)
            : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public DbSet<Especialidad> Especialidades { get; set; }
        public DbSet<ObraSocial> ObrasSociales { get; set; }
        public DbSet<Tratamiento> Tratamientos { get; set; }
        public DbSet<Odontologo> Odontologos { get; set; }
        public DbSet<HorarioAtencion> HorariosAtencion { get; set; }
        public DbSet<Paciente> Pacientes { get; set; }
        public DbSet<Turno> Turnos { get; set; }
        public DbSet<HistoriaClinica> HistoriasClinicas { get; set; }
        public DbSet<Adjunto> Adjuntos { get; set; }
        public DbSet<PiezaDental> PiezasDentales { get; set; }
        public DbSet<LogAuditoria> LogsAuditoria { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Regla general: nada se borra en cascada. Si algo tiene datos asociados, el controlador
            // lo informa; así nunca se pierde una historia clínica por borrar un catálogo.
            builder.Entity<Usuario>().HasIndex(u => u.Dni).IsUnique();

            builder.Entity<Especialidad>().HasIndex(e => e.Nombre).IsUnique();
            builder.Entity<ObraSocial>().HasIndex(o => o.Nombre).IsUnique();

            builder.Entity<Tratamiento>(e =>
            {
                e.Property(t => t.Precio).HasPrecision(10, 2);
                e.HasOne(t => t.Especialidad).WithMany(s => s.Tratamientos)
                    .HasForeignKey(t => t.EspecialidadId).OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Odontologo>(e =>
            {
                e.HasOne(o => o.Usuario).WithMany().HasForeignKey(o => o.UsuarioId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(o => o.Especialidad).WithMany(s => s.Odontologos)
                    .HasForeignKey(o => o.EspecialidadId).OnDelete(DeleteBehavior.Restrict);
                e.HasIndex(o => o.UsuarioId).IsUnique();
                e.HasIndex(o => o.Matricula).IsUnique();
            });

            // Los horarios sí son parte del odontólogo: se borran junto con él.
            builder.Entity<HorarioAtencion>()
                .HasOne(h => h.Odontologo).WithMany(o => o.Horarios)
                .HasForeignKey(h => h.OdontologoId).OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Paciente>(e =>
            {
                e.HasOne(p => p.Usuario).WithMany().HasForeignKey(p => p.UsuarioId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(p => p.ObraSocial).WithMany(o => o.Pacientes)
                    .HasForeignKey(p => p.ObraSocialId).OnDelete(DeleteBehavior.SetNull);
                e.HasIndex(p => p.UsuarioId).IsUnique();
            });

            builder.Entity<Turno>(e =>
            {
                e.HasOne(t => t.Paciente).WithMany(p => p.Turnos).HasForeignKey(t => t.PacienteId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(t => t.Odontologo).WithMany(o => o.Turnos).HasForeignKey(t => t.OdontologoId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(t => t.Tratamiento).WithMany().HasForeignKey(t => t.TratamientoId).OnDelete(DeleteBehavior.Restrict);

                // Red de seguridad contra la doble reserva: aunque dos personas confirmen al mismo
                // tiempo, la base rechaza al segundo. 3 = EstadoTurno.Cancelado (un turno cancelado libera el horario).
                e.HasIndex(t => new { t.OdontologoId, t.FechaHora })
                    .IsUnique()
                    .HasFilter("[Estado] <> 3");
                e.HasIndex(t => t.PacienteId);
            });

            builder.Entity<HistoriaClinica>()
                .HasOne(h => h.Turno).WithOne(t => t.HistoriaClinica)
                .HasForeignKey<HistoriaClinica>(h => h.TurnoId).OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Adjunto>(e =>
            {
                e.HasOne(a => a.Paciente).WithMany(p => p.Adjuntos).HasForeignKey(a => a.PacienteId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(a => a.HistoriaClinica).WithMany(h => h.Adjuntos)
                    .HasForeignKey(a => a.HistoriaClinicaId).OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<PiezaDental>(e =>
            {
                e.HasOne(p => p.Paciente).WithMany(pa => pa.Piezas).HasForeignKey(p => p.PacienteId).OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(p => new { p.PacienteId, p.NumeroPieza }).IsUnique();
            });

            builder.Entity<LogAuditoria>(e =>
            {
                e.HasIndex(l => l.Fecha);
                e.HasIndex(l => l.Entidad);
            });
        }

        // ------------------------------------------------------------------------------------
        // Auditoría. Dos niveles:
        //  1) Campos "creado por / modificado por" en cada AuditableEntity (se ven en las tablas).
        //  2) Una fila en LogAuditoria por cada alta, cambio y baja (con el detalle de qué cambió).
        // Todo se hace acá, en SaveChanges, para que ningún controlador pueda olvidarse de auditar.
        // ------------------------------------------------------------------------------------

        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            AplicarAuditoria();
            var cambios = CapturarCambios();
            if (cambios.Count == 0)
                return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

            // Cambio y registro de auditoría van en la misma transacción (salvo que el llamador ya abrió una).
            await using var propia = Database.CurrentTransaction == null
                ? await Database.BeginTransactionAsync(cancellationToken) : null;

            var filas = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            LogsAuditoria.AddRange(CrearLogs(cambios));
            await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

            if (propia != null) await propia.CommitAsync(cancellationToken);
            return filas;
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            AplicarAuditoria();
            var cambios = CapturarCambios();
            if (cambios.Count == 0)
                return base.SaveChanges(acceptAllChangesOnSuccess);

            using var propia = Database.CurrentTransaction == null ? Database.BeginTransaction() : null;

            var filas = base.SaveChanges(acceptAllChangesOnSuccess);
            LogsAuditoria.AddRange(CrearLogs(cambios));
            base.SaveChanges(acceptAllChangesOnSuccess);

            propia?.Commit();
            return filas;
        }

        private ClaimsPrincipal? UsuarioActual => _httpContextAccessor?.HttpContext?.User;

        private string? UsuarioActualId => UsuarioActual?.FindFirstValue(ClaimTypes.NameIdentifier);

        private void AplicarAuditoria()
        {
            var ahora = Reloj.Ahora;
            var usuarioId = UsuarioActualId;

            foreach (var entrada in ChangeTracker.Entries<AuditableEntity>())
            {
                switch (entrada.State)
                {
                    case EntityState.Added:
                        entrada.Entity.FechaCreacion = ahora;
                        entrada.Entity.UsuarioCreacionId = usuarioId;
                        break;

                    case EntityState.Modified:
                        // Un formulario no puede alterar quién y cuándo creó el registro.
                        entrada.Property(e => e.FechaCreacion).IsModified = false;
                        entrada.Property(e => e.UsuarioCreacionId).IsModified = false;
                        entrada.Entity.FechaModificacion = ahora;
                        entrada.Entity.UsuarioModificacionId = usuarioId;
                        break;
                }
            }
        }

        private sealed class CambioPendiente
        {
            public required EntityEntry Entrada { get; init; }
            public required string Accion { get; init; }
            public required string Entidad { get; init; }
            public string? Clave { get; set; }
            public string? Detalle { get; init; }
        }

        private static readonly HashSet<string> CamposDeAuditoria = new()
        {
            nameof(AuditableEntity.FechaCreacion), nameof(AuditableEntity.UsuarioCreacionId),
            nameof(AuditableEntity.FechaModificacion), nameof(AuditableEntity.UsuarioModificacionId)
        };

        // Se toma "la foto" ANTES de guardar: después de SaveChanges los estados ya se reiniciaron
        // y no se podría saber qué se creó, qué se cambió y qué se borró.
        private List<CambioPendiente> CapturarCambios()
        {
            var lista = new List<CambioPendiente>();

            foreach (var entrada in ChangeTracker.Entries<AuditableEntity>())
            {
                if (entrada.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted)) continue;

                var (accion, detalle) = entrada.State switch
                {
                    EntityState.Added => (AccionAuditoria.Crear, Resumir(entrada, usarOriginales: false)),
                    EntityState.Deleted => (AccionAuditoria.Eliminar, Resumir(entrada, usarOriginales: true)),
                    _ => (AccionAuditoria.Modificar, DescribirCambios(entrada))
                };

                lista.Add(new CambioPendiente
                {
                    Entrada = entrada,
                    Accion = accion,
                    Entidad = entrada.Metadata.ClrType.Name,
                    Detalle = Recortar(detalle, 2000),
                    // En un alta la clave todavía es provisoria; se lee recién después de guardar.
                    Clave = entrada.State == EntityState.Added ? null : ClaveDe(entrada)
                });
            }

            return lista;
        }

        private IEnumerable<LogAuditoria> CrearLogs(List<CambioPendiente> cambios)
        {
            var ahora = Reloj.Ahora;
            var usuarioId = UsuarioActualId;
            var nombre = UsuarioActual?.Identity?.IsAuthenticated == true
                ? UsuarioActual.Identity.Name ?? "Desconocido"
                : "Sistema";

            foreach (var c in cambios)
            {
                yield return new LogAuditoria
                {
                    Fecha = ahora,
                    UsuarioId = usuarioId,
                    UsuarioNombre = Recortar(nombre, 200)!,
                    Accion = c.Accion,
                    Entidad = c.Entidad,
                    EntidadId = c.Clave ?? ClaveDe(c.Entrada),
                    Detalle = c.Detalle
                };
            }
        }

        private static string? ClaveDe(EntityEntry entrada)
        {
            var clave = entrada.Metadata.FindPrimaryKey();
            if (clave == null) return null;
            return string.Join("-", clave.Properties.Select(p => entrada.Property(p.Name).CurrentValue));
        }

        private static string Resumir(EntityEntry entrada, bool usarOriginales)
        {
            var partes = entrada.Properties
                .Where(p => !p.Metadata.IsPrimaryKey() && !CamposDeAuditoria.Contains(p.Metadata.Name))
                .Select(p => $"{p.Metadata.Name}: {Mostrar(usarOriginales ? p.OriginalValue : p.CurrentValue)}");
            return string.Join(" | ", partes);
        }

        private static string DescribirCambios(EntityEntry entrada)
        {
            var partes = entrada.Properties
                .Where(p => p.IsModified && !p.Metadata.IsPrimaryKey() && !CamposDeAuditoria.Contains(p.Metadata.Name))
                .Where(p => !Equals(p.OriginalValue, p.CurrentValue))
                .Select(p => $"{p.Metadata.Name}: {Mostrar(p.OriginalValue)} → {Mostrar(p.CurrentValue)}");
            return string.Join(" | ", partes);
        }

        private static string Mostrar(object? valor) => valor switch
        {
            null => "(vacío)",
            string { Length: 0 } => "(vacío)",
            DateTime f => f.ToString("dd/MM/yyyy HH:mm"),
            DateOnly d => d.ToString("dd/MM/yyyy"),
            TimeOnly t => t.ToString("HH:mm"),
            bool b => b ? "Sí" : "No",
            Enum e => e.ToString(),
            _ => Convert.ToString(valor, System.Globalization.CultureInfo.InvariantCulture) ?? ""
        };

        private static string? Recortar(string? texto, int max) =>
            texto is null || texto.Length <= max ? texto : texto[..(max - 1)] + "…";
    }
}
