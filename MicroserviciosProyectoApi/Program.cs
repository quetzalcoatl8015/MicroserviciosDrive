using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Proyecto.Models;
using Proyecto.Services;
using Scalar.AspNetCore;

var constructor = WebApplication.CreateBuilder(args);

constructor.Services.AddOpenApi();
constructor.Services.AddControllers();
constructor.Services.AddHttpContextAccessor();
constructor.Services.AddScoped<AccesoEquipos>();

var cadenaConexion = constructor.Configuration.GetConnectionString("ConexionPrincipal")
    ?? throw new InvalidOperationException("Falta la cadena de conexión 'ConexionPrincipal' en la configuración.");
constructor.Services.AddDbContext<ContextoProyecto>(opciones => opciones.UseMySQL(cadenaConexion));

constructor.Services.AddSingleton<ServicioJwt>();

constructor.Services.AddSingleton<ServicioAlmacenamiento>();

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
            OnTokenValidated = async contexto =>
            {
                var identidad = contexto.Principal;

                if (identidad == null
                    || identidad.FindFirst("es_acceso")?.Value != "True"
                    || !long.TryParse(identidad.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
                {
                    contexto.Fail("Token inválido.");
                    return;
                }

                var baseDatos = contexto.HttpContext.RequestServices.GetRequiredService<ContextoProyecto>();

                var usuario = await baseDatos.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == usuarioId && u.EstaActivo);
                if (usuario is null)
                {
                    contexto.Fail("El usuario está desactivado.");
                    return;
                }

                if (identidad.FindFirst("sello_sesion")?.Value != usuario.SelloSesion)
                {
                    contexto.Fail("La sesión fue revocada.");
                    return;
                }

                var claims = (ClaimsIdentity)identidad.Identity!;
                foreach (var claim in claims.FindAll(ClaimTypes.Role).ToList()) claims.RemoveClaim(claim);
                claims.AddClaim(new Claim(ClaimTypes.Role, usuario.EsAdmin.ToString()));
                foreach (var claim in claims.FindAll("nivel_admin").ToList()) claims.RemoveClaim(claim);
                claims.AddClaim(new Claim("nivel_admin", usuario.NivelAdmin.ToString()));
                contexto.HttpContext.Items["UsuarioActual"] = usuario;

                return;
            }
        };
    });

constructor.Services.AddAuthorization(opciones =>
{
    opciones.AddPolicy("SoloAdmin", politica => politica.RequireClaim(ClaimTypes.Role, "True"));
    opciones.AddPolicy("AdminGlobal", politica => politica.RequireClaim(ClaimTypes.Role, "True").RequireClaim("nivel_admin", "2"));
});

var nombrePoliticaCors = "_permitirOrigenApp";
constructor.Services.AddCors(opciones =>
{
    opciones.AddPolicy(name: nombrePoliticaCors,
        opcionesCors =>
        {
            opcionesCors.WithOrigins("http://localhost:5173")
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });
});

var aplicacion = constructor.Build();

if (aplicacion.Environment.IsDevelopment())
{
    aplicacion.MapOpenApi();
    aplicacion.MapScalarApiReference();
}

aplicacion.UseCors(nombrePoliticaCors);
aplicacion.UseAuthentication();
aplicacion.UseAuthorization();
aplicacion.MapControllers();

using (var ambito = aplicacion.Services.CreateScope())
{
    var contexto = ambito.ServiceProvider.GetRequiredService<ContextoProyecto>();
    await SembradoBaseDatos.AplicarAsync(contexto, aplicacion.Configuration, aplicacion.Logger);
}

aplicacion.Run();
