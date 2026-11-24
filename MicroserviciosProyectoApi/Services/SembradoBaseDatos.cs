using Microsoft.EntityFrameworkCore;
using Proyecto.Models;

namespace Proyecto.Services;

public static class SembradoBaseDatos
{

    public static async Task AplicarAsync(ContextoProyecto contexto, IConfiguration configuracion, ILogger registro)
    {

        await contexto.Database.MigrateAsync();
        registro.LogInformation("Esquema de la base de datos al día.");

        if (await contexto.Usuarios.AnyAsync())
        {
            registro.LogInformation("La tabla de usuarios ya tiene datos; no se siembra el administrador.");
            return;
        }

        var nombre = configuracion["Sembrado:NombreAdmin"] ?? "Admin";
        var correo = configuracion["Sembrado:CorreoAdmin"] ?? "admin@proyectom.com";
        var contrasena = configuracion["Sembrado:ContrasenaAdmin"] ?? "password";

        var fechaNacimiento = DateOnly.TryParse(configuracion["Sembrado:FechaNacimientoAdmin"], out var fecha)
            ? fecha
            : new DateOnly(1990, 1, 1);

        contexto.Usuarios.Add(new Usuario
        {
            Nombre = nombre,
            Correo = correo,

            Contrasena = BCrypt.Net.BCrypt.HashPassword(contrasena),
            FechaNacimiento = fechaNacimiento,

            EsAdmin = true,
            NivelAdmin = 2,
            EquipoId = 1,
            EstaActivo = true,
        });

        await contexto.SaveChangesAsync();

        registro.LogInformation("Administrador inicial creado con el correo {Correo}.", correo);
    }
}
