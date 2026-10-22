using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Proyecto.Models;

namespace Proyecto.Services;

/// <summary>
/// Servicio que firma y construye los tokens JWT (de acceso y de refresco).
/// Es la única pieza del proyecto que crea tokens, para que las reglas de firma
/// y de contenido estén en un solo lugar.
/// </summary>
/// <remarks>
/// La clave de firma es simétrica (HMAC-SHA256) y se lee de la configuración
/// (<c>ConfiguracionJwt:ClaveSecreta</c>).
/// </remarks>
public class ServicioJwt
{
    private readonly IConfiguration _configuracion;

    public ServicioJwt(IConfiguration configuracion)
    {
        _configuracion = configuracion;
    }

    /// <summary>Minutos de vigencia del token de acceso.</summary>
    public int MinutosTokenAcceso => _configuracion.GetValue("ConfiguracionJwt:MinutosTokenAcceso", 60);

    /// <summary>Minutos de vigencia del token de refresco.</summary>
    public int MinutosTokenRefresco => _configuracion.GetValue("ConfiguracionJwt:MinutosTokenRefresco", 1440);

    /// <summary>Construye el token de acceso (el que viaja en cada petición).</summary>
    public string CrearTokenAcceso(Usuario usuario) => EscribirToken(usuario, esAcceso: true, MinutosTokenAcceso);

    /// <summary>Construye el token de refresco (el que sirve para renovar la sesión).</summary>
    public string CrearTokenRefresco(Usuario usuario) => EscribirToken(usuario, esAcceso: false, MinutosTokenRefresco);

    /// <summary>
    /// Arma un token con los datos del usuario y lo firma.
    /// </summary>
    /// <param name="usuario">Usuario al que pertenece el token.</param>
    /// <param name="esAcceso">
    /// true para el token de acceso, false para el de refresco. Se guarda en el claim
    /// <c>es_acceso</c> para que el middleware de autenticación acepte únicamente los
    /// tokens de acceso: un token de refresco no sirve para llamar a la API.
    /// </param>
    /// <param name="minutos">Minutos de vigencia.</param>
    private string EscribirToken(Usuario usuario, bool esAcceso, int minutos)
    {
        var claims = new List<Claim>
        {
            // Identificador único del token: permite que dos tokens distintos del mismo usuario no coincidan.
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            // Identificador del usuario dueño del token.
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.Nombre),
            // El rol viaja como el texto "True"/"False" y así lo compara la política SoloAdmin.
            new(ClaimTypes.Role, usuario.EsAdmin.ToString()),
            // Marca que distingue un token de acceso de uno de refresco.
            new("es_acceso", esAcceso.ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: _configuracion["ConfiguracionJwt:Emisor"],
            audience: _configuracion["ConfiguracionJwt:Audiencia"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(minutos),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuracion["ConfiguracionJwt:ClaveSecreta"]!)),
                SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
