namespace Proyecto.Models;

public class TokenSesion
{
    public long Id { get; set; }

    public long UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public string Token { get; set; } = null!;

    public DateTime ExpiraEn { get; set; }

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}
