using Microsoft.EntityFrameworkCore;
using Proyecto.Models;

namespace Proyecto.Services;

/// <summary>
/// Prepara la base de datos para que el sistema sea usable desde el primer arranque.
/// </summary>
/// <remarks>
/// Hace dos cosas, siempre en este orden:
///
/// 1. <b>Pone al día el esquema</b> aplicando con EF Core las migraciones que falten.
///    Sin esto, un volumen de base de datos recién creado (por ejemplo el que levanta
///    docker-compose por primera vez) llegaría vacío y cualquier petición fallaría
///    porque las tablas no existirían.
///
/// 2. <b>Siembra el primer administrador</b> cuando la tabla de usuarios está vacía.
///    El autoregistro deja a todo el mundo inactivo y solo un administrador puede
///    activar a los demás, así que sin este sembrado nadie podría entrar nunca:
///    haría falta un administrador para crear al primer administrador.
///
/// El sembrado es idempotente: solo actúa si la tabla está vacía, por lo que
/// reiniciar la API no duplica usuarios ni sobrescribe contraseñas ya cambiadas.
/// </remarks>
public static class SembradoBaseDatos
{
    /// <summary>
    /// Aplica las migraciones pendientes y crea el administrador inicial si no hay usuarios.
    /// </summary>
    /// <param name="contexto">Contexto de EF Core ya construido por el contenedor de servicios.</param>
    /// <param name="configuracion">Origen de las credenciales iniciales (sección <c>Sembrado</c>).</param>
    /// <param name="registro">Registro de lo que ocurre durante el arranque.</param>
    public static async Task AplicarAsync(ContextoProyecto contexto, IConfiguration configuracion, ILogger registro)
    {
        // ---------------------------------------------------------------------
        // Paso 1: esquema al día.
        // MigrateAsync lee la tabla __EFMigrationsHistory y aplica únicamente las
        // migraciones que todavía no se han ejecutado. Si la base ya está al día
        // no hace nada, así que es seguro llamarlo en cada arranque.
        // ---------------------------------------------------------------------
        await contexto.Database.MigrateAsync();
        registro.LogInformation("Esquema de la base de datos al día.");

        // ---------------------------------------------------------------------
        // Paso 2: sembrado del administrador.
        // La comprobación "¿hay algún usuario?" es la que hace idempotente al
        // sembrado: en cuanto exista un usuario (aunque no sea administrador) se
        // respeta el contenido de la tabla y no se crea nada.
        // ---------------------------------------------------------------------
        if (await contexto.Usuarios.AnyAsync())
        {
            registro.LogInformation("La tabla de usuarios ya tiene datos; no se siembra el administrador.");
            return;
        }

        // Las credenciales salen de appsettings.json (sección "Sembrado") para no dejarlas
        // escritas dentro del código. Los valores de aquí son el respaldo por si la
        // sección no existe, de modo que el arranque nunca falle por configuración ausente.
        var nombre = configuracion["Sembrado:NombreAdmin"] ?? "Admin";
        var correo = configuracion["Sembrado:CorreoAdmin"] ?? "admin@proyectom.com";
        var contrasena = configuracion["Sembrado:ContrasenaAdmin"] ?? "password";

        // La fecha de nacimiento es obligatoria en el modelo. Se toma de la
        // configuración en formato ISO (aaaa-mm-dd) y, si no viene, se usa una fija.
        var fechaNacimiento = DateOnly.TryParse(configuracion["Sembrado:FechaNacimientoAdmin"], out var fecha)
            ? fecha
            : new DateOnly(1990, 1, 1);

        contexto.Usuarios.Add(new Usuario
        {
            Nombre = nombre,
            Correo = correo,
            // La contraseña se guarda con el mismo BCrypt que usa el inicio de sesión, nunca en claro.
            Contrasena = BCrypt.Net.BCrypt.HashPassword(contrasena),
            FechaNacimiento = fechaNacimiento,
            // Único usuario que nace activo y administrador: es la puerta de entrada.
            EsAdmin = true,
            EstaActivo = true,
        });

        await contexto.SaveChangesAsync();

        registro.LogInformation("Administrador inicial creado con el correo {Correo}.", correo);
    }
}
