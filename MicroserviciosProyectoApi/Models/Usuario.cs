namespace Proyecto.Models;

/// <summary>
/// Cuenta de un usuario del repositorio. Corresponde a la tabla "Usuarios".
/// </summary>
public class Usuario
{
    public long Id { get; set; }

    /// <summary>Nombre completo del usuario.</summary>
    public string Nombre { get; set; } = null!;

    /// <summary>Correo con el que inicia sesión. Es único en toda la tabla.</summary>
    public string Correo { get; set; } = null!;

    /// <summary>
    /// Contraseña codificada con BCrypt (nunca en claro). Se llama Contrasena y no
    /// "Password" porque el proyecto completo está en español.
    /// </summary>
    public string Contrasena { get; set; } = null!;

    /// <summary>Fecha de nacimiento. En la base de datos es una columna DATE.</summary>
    public DateOnly FechaNacimiento { get; set; }

    /// <summary>true = administrador; false = usuario común.</summary>
    public bool EsAdmin { get; set; }

    /// <summary>
    /// false al autoregistrarse: un administrador debe activar la cuenta antes de
    /// que la persona pueda iniciar sesión.
    /// </summary>
    public bool EstaActivo { get; set; }

    /// <summary>Momento en que se creó el registro.</summary>
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    /// <summary>Momento de la última modificación.</summary>
    public DateTime ActualizadoEn { get; set; } = DateTime.UtcNow;

    /// <summary>Cada usuario puede tener cuando mucho un token de sesión asignado.</summary>
    public TokenSesion? TokenSesion { get; set; }
}
