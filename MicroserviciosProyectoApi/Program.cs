using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Proyecto.Models;
using Proyecto.Services;

// Punto de entrada de la API. Aquí se registran los servicios y se arma la tubería
// (middleware) por la que pasan todas las peticiones. Todo el archivo está en español
// igual que el resto del backend.
//
// Esta API es una API pura: no expone ninguna interfaz gráfica. Se retiraron OpenAPI y
// Scalar porque mostraban una página interactiva para probar los puntos de conexión, y
// el proyecto exige que el backend no contenga interfaz de usuario de ningún tipo.
var constructor = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Servicios de la aplicación
// ---------------------------------------------------------------------------

constructor.Services.AddControllers();

// Base de datos MySQL a través de EF Core. La cadena de conexión se lee de la configuración
// con la clave "ConnectionStrings:ConexionPrincipal". Si no existe, se aborta el arranque con
// un mensaje claro en lugar de dejar que más adelante falle con un valor nulo.
var cadenaConexion = constructor.Configuration.GetConnectionString("ConexionPrincipal")
    ?? throw new InvalidOperationException("Falta la cadena de conexión 'ConexionPrincipal' en la configuración.");
constructor.Services.AddDbContext<ContextoProyecto>(opciones => opciones.UseMySQL(cadenaConexion));

// Servicio que firma los JWT (tokens de acceso y de refresco).
constructor.Services.AddSingleton<ServicioJwt>();

// Almacenamiento físico de los archivos subidos. Se registra como singleton porque solo
// resuelve la ruta raíz una vez y no mantiene estado por petición.
constructor.Services.AddSingleton<ServicioAlmacenamiento>();

// ---------------------------------------------------------------------------
// Autenticación con JWT
// ---------------------------------------------------------------------------
constructor.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opciones =>
    {
        opciones.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = constructor.Configuration["ConfiguracionJwt:Emisor"],
            ValidAudience = constructor.Configuration["ConfiguracionJwt:Audiencia"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(constructor.Configuration["ConfiguracionJwt:ClaveSecreta"]!)),
        };

        opciones.Events = new JwtBearerEvents
        {
            OnTokenValidated = contexto =>
            {
                var identidad = contexto.Principal;

                // Únicamente los tokens de acceso sirven para autenticar peticiones: un token
                // de refresco (cuyo claim "es_acceso" es "False") se rechaza aquí.
                if (identidad == null
                    || identidad.FindFirst("es_acceso")?.Value != "True"
                    || !long.TryParse(identidad.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
                {
                    contexto.Fail("Token inválido.");
                    return Task.CompletedTask;
                }

                // Un usuario desactivado pierde el acceso de inmediato aunque su token siga vigente.
                var baseDatos = contexto.HttpContext.RequestServices.GetRequiredService<ContextoProyecto>();

                if (!baseDatos.Usuarios.Any(u => u.Id == usuarioId && u.EstaActivo))
                    contexto.Fail("El usuario está desactivado.");

                return Task.CompletedTask;
            }
        };
    });

// Política de autorización para el módulo de usuarios: solo administradores.
// El rol viaja en el claim como el texto "True"/"False".
constructor.Services.AddAuthorization(opciones =>
{
    opciones.AddPolicy("SoloAdmin", politica => politica.RequireClaim(ClaimTypes.Role, "True"));
});

// ---------------------------------------------------------------------------
// CORS (intercambio de recursos entre distintos orígenes)
// ---------------------------------------------------------------------------
// El frontend se sirve desde un origen distinto al de la API (otro puerto, y en
// producción otra máquina), así que el navegador bloquea las llamadas salvo que la API
// declare explícitamente qué orígenes acepta.
//
// Los orígenes se leen de la configuración en "Cors:OrigenesPermitidos" para no dejar la
// dirección del frontend escrita dentro del código: en Docker se pasan como variables de
// entorno y así el mismo binario sirve en local y en cualquier servidor.
//
// Si la sección no existe se usa el origen del servidor de desarrollo de Vite, que es el
// caso de quien trabaja con "dotnet run" y "npm run dev".
var origenesPermitidos = constructor.Configuration.GetSection("Cors:OrigenesPermitidos").Get<string[]>();

if (origenesPermitidos is null || origenesPermitidos.Length == 0)
{
    origenesPermitidos = ["http://localhost:5173"];
}

var nombrePoliticaCors = "_permitirOrigenApp";
constructor.Services.AddCors(opciones =>
{
    opciones.AddPolicy(name: nombrePoliticaCors,
        opcionesCors =>
        {
            opcionesCors.WithOrigins(origenesPermitidos)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });
});

var aplicacion = constructor.Build();

// ---------------------------------------------------------------------------
// Tubería de peticiones
// ---------------------------------------------------------------------------
aplicacion.UseCors(nombrePoliticaCors);
aplicacion.UseAuthentication();
aplicacion.UseAuthorization();
aplicacion.MapControllers();

// Preparación de la base de datos antes de atender peticiones: aplica las migraciones
// pendientes y siembra el administrador inicial la primera vez (ver SembradoBaseDatos).
// Se abre un ámbito propio porque los servicios con ámbito (el DbContext) no se pueden
// resolver directamente desde la raíz del contenedor.
using (var ambito = aplicacion.Services.CreateScope())
{
    var contexto = ambito.ServiceProvider.GetRequiredService<ContextoProyecto>();
    await SembradoBaseDatos.AplicarAsync(contexto, aplicacion.Configuration, aplicacion.Logger);
}

aplicacion.Run();
