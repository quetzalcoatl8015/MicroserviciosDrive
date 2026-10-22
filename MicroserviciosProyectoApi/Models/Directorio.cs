namespace Proyecto.Models;

/// <summary>
/// Directorio del repositorio. Forma un árbol: un directorio puede estar en la raíz
/// (PadreId nulo) o dentro de otro directorio. Corresponde a la tabla "Directorios".
/// </summary>
/// <remarks>
/// Ya no se necesita el alias <c>using Directorio = Proyecto.Models.Directorio;</c> que
/// hacía falta cuando la clase se llamaba <c>Directory</c>, porque ese nombre chocaba con
/// <see cref="System.IO.Directory"/>. Al llamarse Directorio no hay ambigüedad.
/// </remarks>
public class Directorio
{
    public long Id { get; set; }

    /// <summary>Nombre del directorio, sin rutas. Su unicidad se valida por nivel (padre + nombre).</summary>
    public string Nombre { get; set; } = null!;

    /// <summary>Directorio padre. Es nulo cuando el directorio está en la raíz del repositorio.</summary>
    public long? PadreId { get; set; }
    public Directorio? Padre { get; set; }

    /// <summary>Subdirectorios contenidos en este directorio.</summary>
    public ICollection<Directorio> Subdirectorios { get; set; } = new List<Directorio>();

    /// <summary>Usuario que creó el directorio. Solo él (o un administrador) puede renombrarlo o eliminarlo.</summary>
    public long PropietarioId { get; set; }
    public Usuario Propietario { get; set; } = null!;

    /// <summary>Archivos almacenados dentro de este directorio.</summary>
    public ICollection<Archivo> Archivos { get; set; } = new List<Archivo>();

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
    public DateTime ActualizadoEn { get; set; } = DateTime.UtcNow;
}
