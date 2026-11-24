namespace Proyecto.Models;

public class Archivo
{
    public long Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string Extension { get; set; } = null!;

    public string NombreAlmacenamiento { get; set; } = null!;

    public long TamanoBytes { get; set; }

    public string TipoContenido { get; set; } = null!;

    public long DirectorioId { get; set; }
    public Directorio Directorio { get; set; } = null!;

    public long PropietarioId { get; set; }
    public Usuario Propietario { get; set; } = null!;

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
    public DateTime ActualizadoEn { get; set; } = DateTime.UtcNow;
}
