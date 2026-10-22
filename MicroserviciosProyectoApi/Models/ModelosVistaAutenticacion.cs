using System.ComponentModel.DataAnnotations;

namespace Proyecto.Models;

/// <summary>Cuerpo del inicio de sesión.</summary>
public class CredencialesAcceso
{
    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El formato del correo no es válido.")]
    public string Correo { get; set; } = null!;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    public string Contrasena { get; set; } = null!;
}

/// <summary>
/// Cuerpo del autoregistro. La cuenta queda inactiva y como usuario común hasta que
/// un administrador la active.
/// </summary>
public class ModeloRegistro : IValidatableObject
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

    /// <summary>Validación cruzada: las dos contraseñas deben coincidir.</summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Contrasena != ConfirmarContrasena)
            yield return new ValidationResult("Las contraseñas deben coincidir", [nameof(ConfirmarContrasena)]);
    }
}

/// <summary>Cuerpo de la renovación de sesión: solo lleva el token de refresco.</summary>
public class SolicitudTokenRefresco
{
    [Required(ErrorMessage = "El token de refresco es obligatorio.")]
    public string TokenRefresco { get; set; } = null!;
}

/// <summary>Par de tokens entregado al cliente tras iniciar sesión o renovar la sesión.</summary>
public record TokensAcceso(string TokenAcceso, string TokenRefresco);
