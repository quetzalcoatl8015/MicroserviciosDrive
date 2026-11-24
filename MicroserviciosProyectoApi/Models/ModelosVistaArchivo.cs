using System.ComponentModel.DataAnnotations;

namespace Proyecto.Models;

public class ModeloCrearDirectorio
{
    public long? EquipoId { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [MaxLength(200)]
    [RegularExpression(@"^[^\\/:*?""<>|]+$", ErrorMessage = "El nombre contiene caracteres no permitidos.")]
    public string Nombre { get; set; } = null!;

    public long? PadreId { get; set; }
}

public class ModeloRenombrar
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [MaxLength(200)]
    [RegularExpression(@"^[^\\/:*?""<>|]+$", ErrorMessage = "El nombre contiene caracteres no permitidos.")]
    public string Nombre { get; set; } = null!;
}

public class ModeloSubirArchivo
{
    [Required(ErrorMessage = "El archivo es obligatorio.")]
    public IFormFile? Archivo { get; set; }

    [Required(ErrorMessage = "El directorio destino es obligatorio.")]
    public long? DirectorioId { get; set; }
}

public class DirectorioListado
{
    public long EquipoId { get; set; }

    public long Id { get; set; }
    public string Nombre { get; set; } = null!;
    public long? PadreId { get; set; }
    public long PropietarioId { get; set; }

    public string Propietario { get; set; } = null!;

    public int CantidadArchivos { get; set; }

    public int CantidadSubdirectorios { get; set; }

    public bool EstaVacio { get; set; }

    public DateTime CreadoEn { get; set; }
    public DateTime ActualizadoEn { get; set; }
}

public class ArchivoListado
{
    public long EquipoId { get; set; }

    public long Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string Extension { get; set; } = null!;

    public string NombreCompleto { get; set; } = null!;

    public long TamanoBytes { get; set; }
    public string TipoContenido { get; set; } = null!;

    public long DirectorioId { get; set; }

    public string NombreDirectorio { get; set; } = null!;

    public long PropietarioId { get; set; }

    public string Propietario { get; set; } = null!;

    public DateTime CreadoEn { get; set; }
    public DateTime ActualizadoEn { get; set; }
}
