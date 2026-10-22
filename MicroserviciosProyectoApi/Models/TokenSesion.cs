namespace Proyecto.Models;

/// <summary>
/// Relación de qué usuario tiene asignado qué token de refresco (Refresh Token).
/// Corresponde a la tabla "TokensSesion".
/// </summary>
public class TokenSesion
{
    public long Id { get; set; }

    /// <summary>Usuario dueño del token. Es único: un solo token vigente por usuario.</summary>
    public long UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    /// <summary>
    /// Hash SHA-256 (hexadecimal) del token de refresco. No se guarda el token en claro:
    /// así la tabla no contiene credenciales reutilizables y el tamaño es fijo (64).
    /// </summary>
    public string Token { get; set; } = null!;

    /// <summary>Momento en que el token deja de ser válido.</summary>
    public DateTime ExpiraEn { get; set; }

    /// <summary>Momento en que se emitió.</summary>
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}
