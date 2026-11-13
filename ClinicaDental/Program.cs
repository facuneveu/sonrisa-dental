using ClinicaDental.Data;
using ClinicaDental.Models;
using ClinicaDental.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

if (Environment.GetEnvironmentVariable("PORT") is { Length: > 0 } puerto)
    builder.WebHost.UseUrls($"http://0.0.0.0:{puerto}");

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddAntiforgery(o => o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest);

var rutaClaves = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(rutaClaves))
{
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(Path.GetFullPath(rutaClaves, builder.Environment.ContentRootPath)))
        .SetApplicationName("ClinicaDental");
}

builder.Services.AddIdentity<Usuario, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.User.RequireUniqueEmail = true;

    options.Lockout.MaxFailedAccessAttempts = 10;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddErrorDescriber<IdentityErrorDescriberEs>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

builder.Services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.FromMinutes(1));

builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<DisponibilidadService>();
builder.Services.AddScoped<PerfilService>();
builder.Services.AddScoped<NombresUsuarioService>();
builder.Services.AddScoped<MensajesService>();
builder.Services.AddSingleton<ArchivoService>();

builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection(GeminiOptions.Seccion));
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<GeminiClient>((sp, cliente) =>
{
    var opciones = sp.GetRequiredService<IOptions<GeminiOptions>>().Value;
    cliente.BaseAddress = new Uri(opciones.BaseUrl.EndsWith('/') ? opciones.BaseUrl : opciones.BaseUrl + "/");
    cliente.Timeout = Timeout.InfiniteTimeSpan;
});
builder.Services.AddScoped<AsistenteService>();

var consultasPorMinuto = Math.Max(1, builder.Configuration.GetValue("Asistente:ConsultasPorMinuto", 8));
builder.Services.AddRateLimiter(opciones =>
{
    opciones.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    opciones.AddPolicy("asistente", contexto => RateLimitPartition.GetFixedWindowLimiter(
        contexto.User.Identity?.Name ?? contexto.Connection.RemoteIpAddress?.ToString() ?? "anonimo",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = consultasPorMinuto, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    opciones.OnRejected = async (contexto, ct) =>
    {
        contexto.HttpContext.Response.ContentType = "application/json";
        await contexto.HttpContext.Response.WriteAsync(
            "{\"ok\":false,\"texto\":\"Estás consultando muy seguido. Esperá un minuto y probá de nuevo.\"}", ct);
    };
});

builder.Services.AddControllersWithViews(options =>
{
    var m = options.ModelBindingMessageProvider;
    m.SetValueMustNotBeNullAccessor(_ => "Este campo es obligatorio.");
    m.SetMissingBindRequiredValueAccessor(campo => $"Falta el valor de '{campo}'.");
    m.SetMissingKeyOrValueAccessor(() => "Falta un valor.");
    m.SetMissingRequestBodyRequiredValueAccessor(() => "Falta el cuerpo de la solicitud.");
    m.SetValueIsInvalidAccessor(valor => $"El valor '{valor}' no es válido.");
    m.SetAttemptedValueIsInvalidAccessor((valor, campo) => $"El valor '{valor}' no es válido para {campo}.");
    m.SetNonPropertyAttemptedValueIsInvalidAccessor(valor => $"El valor '{valor}' no es válido.");
    m.SetUnknownValueIsInvalidAccessor(campo => $"El valor ingresado no es válido para {campo}.");
    m.SetNonPropertyUnknownValueIsInvalidAccessor(() => "El valor ingresado no es válido.");
    m.SetValueMustBeANumberAccessor(campo => $"{campo} debe ser un número.");
    m.SetNonPropertyValueMustBeANumberAccessor(() => "El valor debe ser un número.");
});
builder.Services.AddRazorPages();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await services.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();
        await Inicializador.CrearRolesYAdministradorAsync(services);

        if (builder.Configuration.GetValue("SeedDatosEjemplo", true))
        {
            await DatosEjemplo.SembrarAsync(services);
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ocurrió un error al crear la base de datos, los roles o los datos iniciales.");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.Use(async (contexto, siguiente) =>
{
    var cabeceras = contexto.Response.Headers;
    cabeceras["X-Content-Type-Options"] = "nosniff";
    cabeceras["X-Frame-Options"] = "DENY";
    cabeceras["Referrer-Policy"] = "same-origin";
    await siguiente();
});

app.UseStaticFiles();


app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("en-US"),
    SupportedCultures = [new System.Globalization.CultureInfo("en-US")],
    SupportedUICultures = [new System.Globalization.CultureInfo("en-US")]
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapGet("/healthz", () => Results.Text("ok"));

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
