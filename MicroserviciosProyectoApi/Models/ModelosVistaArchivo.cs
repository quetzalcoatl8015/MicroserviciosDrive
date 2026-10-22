using System.ComponentModel.DataAnnotations;

namespace Proyecto.Models;

/// <summary>Cuerpo para crear un directorio (requerimiento 3.1).</summary>
public class ModeloCrearDirectorio
{
    /// <summary>
    /// Nombre del directorio. La expresión regular impide separadores de ruta y
    /// caracteres reservados, de modo que un nombre nunca pueda escapar del almacenamiento.
    /// </summary>
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [MaxLength(200)]
    [RegularExpression(@"^[^\\/:*?""<>|]+$", ErrorMessage = "El nombre contiene caracteres no permitidos.")]
    public string Nombre { get; set; } = null!;

    /// <summary>Directorio padre. Se omite o se envía nulo para crearlo en la raíz.</summary>
    public long? PadreId { get; set; }
}

/// <summary>Cuerpo para renombrar un archivo o un directorio (requerimiento 3.4).</summary>
public class ModeloRenombrar
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [MaxLength(200)]
    [RegularExpression(@"^[^\\/:*?""<>|]+$", ErrorMessage = "El nombre contiene caracteres no permitidos.")]
    public string Nombre { get; set; } = null!;
}

/// <summary>
/// Cuerpo del formulario de subida (multipart/form-data, requerimiento 3.2).
/// El archivo viaja en la parte "Archivo" y el destino en la parte "DirectorioId".
/// </summary>
public class ModeloSubirArchivo
{
    [Required(ErrorMessage = "El archivo es obligatorio.")]
    public IFormFile? Archivo { get; set; }

    /// <summary>Directorio destino. Es obligatorio: los archivos no pueden subirse en la raíz.</summary>
    [Required(ErrorMessage = "El directorio destino es obligatorio.")]
    public long? DirectorioId { get; set; }
}

/// <summary>Directorio tal como lo expone la API (requerimiento 3.5).</summary>
public class DirectorioListado
{
    public long Id { get; set; }
    public string Nombre { get; set; } = null!;
    public long? PadreId { get; set; }
    public long PropietarioId { get; set; }

    /// <summary>Nombre del usuario que creó el directorio.</summary>
    public string Propietario { get; set; } = null!;

    /// <summary>Cantidad de archivos dentro del directorio.</summary>
    public int CantidadArchivos { get; set; }

    /// <summary>Cantidad de subdirectorios dentro del directorio.</summary>
    public int CantidadSubdirectorios { get; set; }

    /// <summary>
    /// Indica si el directorio está vacío. El cliente lo usa para saber si un usuario
    /// común puede eliminarlo (solo los vacíos) o si hace falta ser administrador.
    /// </summary>
    public bool EstaVacio { get; set; }

    public DateTime CreadoEn { get; set; }
    public DateTime ActualizadoEn { get; set; }
}

/// <summary>Archivo tal como lo expone la API (requerimiento 3.5).</summary>
public class ArchivoListado
{
    public long Id { get; set; }

    /// <summary>Nombre sin extensión.</summary>
    public string Nombre { get; set; } = null!;

    /// <summary>Extensión con el punto incluido.</summary>
    public string Extension { get; set; } = null!;

    /// <summary>Nombre completo (nombre + extensión), tal como lo ve el usuario.</summary>
    public string NombreCompleto { get; set; } = null!;

    public long TamanoBytes { get; set; }
    public string TipoContenido { get; set; } = null!;

    public long DirectorioId { get; set; }

    /// <summary>Nombre del directorio que contiene el archivo.</summary>
    public string NombreDirectorio { get; set; } = null!;

    public long PropietarioId { get; set; }

    /// <summary>Nombre del usuario que subió el archivo.</summary>
    public string Propietario { get; set; } = null!;

    /// <summary>Fecha en la que se subió el archivo.</summary>
    public DateTime CreadoEn { get; set; }
    public DateTime ActualizadoEn { get; set; }
}
