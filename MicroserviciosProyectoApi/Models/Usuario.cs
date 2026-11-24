namespace Proyecto.Models;

public class Usuario
{
    public long Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string Correo { get; set; } = null!;

    public string Contrasena { get; set; } = null!;

    public DateOnly FechaNacimiento { get; set; }

    public bool EsAdmin { get; set; }

    public int NivelAdmin { get; set; }

    public long? EquipoId { get; set; }

    public Equipo? Equipo { get; set; }

    public bool EstaActivo { get; set; }

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    public DateTime ActualizadoEn { get; set; } = DateTime.UtcNow;

    public string SelloSesion { get; set; } = Guid.NewGuid().ToString();

    public TokenSesion? TokenSesion { get; set; }
}
