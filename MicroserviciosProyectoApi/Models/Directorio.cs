namespace Proyecto.Models;

public class Directorio
{
    public long Id { get; set; }
    public long EquipoId { get; set; }
    public Equipo Equipo { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public long? PadreId { get; set; }
    public Directorio? Padre { get; set; }

    public ICollection<Directorio> Subdirectorios { get; set; } = new List<Directorio>();

    public long PropietarioId { get; set; }
    public Usuario Propietario { get; set; } = null!;

    public ICollection<Archivo> Archivos { get; set; } = new List<Archivo>();

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
    public DateTime ActualizadoEn { get; set; } = DateTime.UtcNow;
}
