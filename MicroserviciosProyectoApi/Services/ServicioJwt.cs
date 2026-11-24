using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Proyecto.Models;

namespace Proyecto.Services;

public class ServicioJwt
{
    private readonly IConfiguration _configuracion;

    public ServicioJwt(IConfiguration configuracion)
    {
        _configuracion = configuracion;
    }

    public int MinutosTokenAcceso => _configuracion.GetValue("ConfiguracionJwt:MinutosTokenAcceso", 60);

    public int MinutosTokenRefresco => _configuracion.GetValue("ConfiguracionJwt:MinutosTokenRefresco", 1440);

    public string CrearTokenAcceso(Usuario usuario) => EscribirToken(usuario, esAcceso: true, MinutosTokenAcceso);

    public string CrearTokenRefresco(Usuario usuario) => EscribirToken(usuario, esAcceso: false, MinutosTokenRefresco);

    private string EscribirToken(Usuario usuario, bool esAcceso, int minutos)
    {
        var claims = new List<Claim>
        {

            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),

            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.Nombre),

            new(ClaimTypes.Role, usuario.EsAdmin.ToString()),
            new("nivel_admin", usuario.NivelAdmin.ToString()),
            new("equipo_id", usuario.EquipoId?.ToString() ?? string.Empty),

            new("sello_sesion", usuario.SelloSesion),

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
