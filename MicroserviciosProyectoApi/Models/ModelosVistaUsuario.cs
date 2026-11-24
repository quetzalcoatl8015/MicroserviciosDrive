using System.ComponentModel.DataAnnotations;

namespace Proyecto.Models;

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

    public bool EsAdmin { get; set; }
    [Range(0, 2)]
    public int NivelAdmin { get; set; }
    public long? EquipoId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Contrasena != ConfirmarContrasena)
            yield return new ValidationResult("Las contraseñas deben coincidir", [nameof(ConfirmarContrasena)]);
    }
}

public class ModeloEditarUsuario : IValidatableObject
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    public string Nombre { get; set; } = null!;

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El formato del correo no es válido.")]
    public string Correo { get; set; } = null!;

    public string? Contrasena { get; set; }

    public string? ConfirmarContrasena { get; set; }

    [Required(ErrorMessage = "La fecha de nacimiento es obligatoria.")]
    public DateOnly? FechaNacimiento { get; set; }

    public bool EsAdmin { get; set; }
    [Range(0, 2)]
    public int NivelAdmin { get; set; }
    public long? EquipoId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var hayContrasena = !string.IsNullOrEmpty(Contrasena);
        var hayConfirmacion = !string.IsNullOrEmpty(ConfirmarContrasena);

        if (!hayContrasena && !hayConfirmacion)
            yield break;

        if (!hayContrasena || !hayConfirmacion)
            yield return new ValidationResult("Debes proporcionar la contraseña dos veces", [nameof(ConfirmarContrasena)]);
        else if (Contrasena != ConfirmarContrasena)
            yield return new ValidationResult("Las contraseñas deben coincidir", [nameof(ConfirmarContrasena)]);
    }
}

public class ModeloCambiarEstadoUsuario
{
    public bool EstaActivo { get; set; }

    public long? EquipoId { get; set; }
}

public class UsuarioListado
{
    public long Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string Correo { get; set; } = null!;
    public DateOnly FechaNacimiento { get; set; }
    public bool EsAdmin { get; set; }
    [Range(0, 2)]
    public int NivelAdmin { get; set; }
    public long? EquipoId { get; set; }

    public string? Equipo { get; set; }
    public bool EstaActivo { get; set; }

    public bool TieneTokenSesion { get; set; }

    public DateTime CreadoEn { get; set; }
    public DateTime ActualizadoEn { get; set; }
}
