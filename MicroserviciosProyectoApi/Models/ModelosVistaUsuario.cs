using System.ComponentModel.DataAnnotations;

namespace Proyecto.Models;

/// <summary>Cuerpo para que un administrador cree un usuario (requerimiento 2.1).</summary>
public class ModeloCrearUsuario : IValidatableObject
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    public string Nombre { get; set; } = null!;

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El formato del correo no es válido.")]
    public string Correo { get; set; } = null!;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    public string Contrasena { get; set; } = null!;

    [Required(ErrorMessage = "Debes confirmar la contraseña.")]
    public string ConfirmarContrasena { get; set; } = null!;

    [Required(ErrorMessage = "La fecha de nacimiento es obligatoria.")]
    public DateOnly? FechaNacimiento { get; set; }

    /// <summary>Solo un administrador puede asignar el rol de administrador.</summary>
    public bool EsAdmin { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Contrasena != ConfirmarContrasena)
            yield return new ValidationResult("Las contraseñas deben coincidir", [nameof(ConfirmarContrasena)]);
    }
}

/// <summary>Cuerpo para editar un usuario (requerimiento 2.2).</summary>
public class ModeloEditarUsuario : IValidatableObject
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    public string Nombre { get; set; } = null!;

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El formato del correo no es válido.")]
    public string Correo { get; set; } = null!;

    /// <summary>Opcional. Si se envía, también debe enviarse ConfirmarContrasena.</summary>
    public string? Contrasena { get; set; }

    public string? ConfirmarContrasena { get; set; }

    [Required(ErrorMessage = "La fecha de nacimiento es obligatoria.")]
    public DateOnly? FechaNacimiento { get; set; }

    public bool EsAdmin { get; set; }

    /// <summary>
    /// La contraseña es opcional al editar. Aquí se valida que, si se manda una, se mande
    /// también su confirmación y que ambas coincidan.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var hayContrasena = !string.IsNullOrEmpty(Contrasena);
        var hayConfirmacion = !string.IsNullOrEmpty(ConfirmarContrasena);

        // Si no se envía ninguna de las dos, no hay nada que validar (no se cambia la contraseña).
        if (!hayContrasena && !hayConfirmacion)
            yield break;

        if (!hayContrasena || !hayConfirmacion)
            yield return new ValidationResult("Debes proporcionar la contraseña dos veces", [nameof(ConfirmarContrasena)]);
        else if (Contrasena != ConfirmarContrasena)
            yield return new ValidationResult("Las contraseñas deben coincidir", [nameof(ConfirmarContrasena)]);
    }
}

/// <summary>Cuerpo para activar o desactivar un usuario (requerimiento 2.3).</summary>
public class ModeloCambiarEstadoUsuario
{
    public bool EstaActivo { get; set; }
}

/// <summary>Usuario expuesto por la API (nunca incluye la contraseña).</summary>
public class UsuarioListado
{
    public long Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string Correo { get; set; } = null!;
    public DateOnly FechaNacimiento { get; set; }
    public bool EsAdmin { get; set; }
    public bool EstaActivo { get; set; }

    /// <summary>Indica si el usuario tiene un token de refresco asignado (sesión abierta).</summary>
    public bool TieneTokenSesion { get; set; }

    public DateTime CreadoEn { get; set; }
    public DateTime ActualizadoEn { get; set; }
}
