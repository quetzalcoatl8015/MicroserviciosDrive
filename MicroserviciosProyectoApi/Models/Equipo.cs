using System.ComponentModel.DataAnnotations;

namespace Proyecto.Models;

public class Equipo
{
    public long Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string CodigoRegistro { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}

public class ContenidoCompartido
{
    public long Id { get; set; }
    public long EquipoDestinoId { get; set; }
    public Equipo EquipoDestino { get; set; } = null!;
    public long? ArchivoId { get; set; }
    public Archivo? Archivo { get; set; }
    public long? DirectorioId { get; set; }
    public Directorio? Directorio { get; set; }
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}

public class ModeloEquipo
{
    [Required, MaxLength(100)]
    public string Nombre { get; set; } = null!;
}

public class ModeloCompartir : IValidatableObject
{
    [Range(1, long.MaxValue)]
    public long EquipoDestinoId { get; set; }
    public long? ArchivoId { get; set; }
    public long? DirectorioId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ArchivoId.HasValue == DirectorioId.HasValue)
            yield return new ValidationResult("Indica un archivo o una carpeta, exclusivamente.");
    }
}
