namespace Proyecto.Models;

/// <summary>
/// Archivo subido al repositorio. Corresponde a la tabla "Archivos".
/// </summary>
/// <remarks>
/// La clase se llama <c>Archivo</c> y no <c>File</c> para no chocar con
/// <see cref="System.IO.File"/>, que es la clase que el sistema operativo usa para
/// escribir en el disco.
///
/// El nombre y la extensión se guardan en columnas separadas a propósito: así el
/// renombrado (que solo escribe en <see cref="Nombre"/>) no puede alterar la extensión,
/// tal como lo exige el requerimiento 3.4.
/// </remarks>
public class Archivo
{
    public long Id { get; set; }

    /// <summary>Nombre del archivo SIN la extensión.</summary>
    public string Nombre { get; set; } = null!;

    /// <summary>Extensión con el punto incluido (por ejemplo ".pdf"). Puede ser cadena vacía.</summary>
    public string Extension { get; set; } = null!;

    /// <summary>
    /// Nombre real del archivo en el almacenamiento físico (GUID + extensión).
    /// El GUID evita que dos archivos con el mismo nombre se sobrescriban en el disco.
    /// </summary>
    public string NombreAlmacenamiento { get; set; } = null!;

    /// <summary>Tamaño del archivo en bytes. En la base de datos es un bigint.</summary>
    public long TamanoBytes { get; set; }

    /// <summary>Tipo MIME declarado por el cliente al subir el archivo (por ejemplo "text/plain").</summary>
    public string TipoContenido { get; set; } = null!;

    /// <summary>Directorio que contiene el archivo. Un archivo nunca puede estar en la raíz.</summary>
    public long DirectorioId { get; set; }
    public Directorio Directorio { get; set; } = null!;

    /// <summary>Usuario que subió el archivo. Solo él (o un administrador) puede renombrarlo o eliminarlo.</summary>
    public long PropietarioId { get; set; }
    public Usuario Propietario { get; set; } = null!;

    /// <summary>Fecha y hora en que se subió el archivo.</summary>
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
    public DateTime ActualizadoEn { get; set; } = DateTime.UtcNow;
}
