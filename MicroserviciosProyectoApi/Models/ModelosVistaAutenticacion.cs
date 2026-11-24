using System.ComponentModel.DataAnnotations;

namespace Proyecto.Models;

public class CredencialesAcceso
{
    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El formato del correo no es válido.")]
    public string Correo { get; set; } = null!;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    public string Contrasena { get; set; } = null!;
}

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

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Contrasena != ConfirmarContrasena)
            yield return new ValidationResult("Las contraseñas deben coincidir", [nameof(ConfirmarContrasena)]);
    }
}

public class SolicitudTokenRefresco
{
    [Required(ErrorMessage = "El token de refresco es obligatorio.")]
    public string TokenRefresco { get; set; } = null!;
}

public record TokensAcceso(string TokenAcceso, string TokenRefresco);
