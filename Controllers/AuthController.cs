using Microsoft.AspNetCore.Mvc;
using Proyecto.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Proyecto.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly ProyectoContext _context;
    private readonly IConfiguration _config;

    public AuthController(ProyectoContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(UserCredentials userCredentials)
    {
        if(ModelState.IsValid)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == userCredentials.Email);

            if(user == null || Models.User.GetHash(userCredentials.Password) != user.Password)
                return Unauthorized();

            
            JwtSecurityTokenHandler tokenHandler = new();

            return Ok(new {
                AccessToken = tokenHandler.WriteToken(GenerateAccessToken(user)),
                RefreshToken = tokenHandler.WriteToken(GenerateRefreshToken(user)),
            });
        }

        return Unauthorized();
    }

    [HttpPost("getNewAccessToken")]
    public async Task<IActionResult> GetNewAccessToken(StringAccessToken refreshToken)
    {
        JwtSecurityTokenHandler tokenHandler = new();

        var jwtToken = tokenHandler.ReadJwtToken(refreshToken.AccessToken);
        var claims = jwtToken.Claims.ToDictionary(c => c.Type, c => c.Value);

        if(!claims["is_access"].Equals("false", StringComparison.CurrentCultureIgnoreCase))
            return BadRequest();

        var user = _context.Users.FirstOrDefault(u => u.Id == Convert.ToInt32(claims[ClaimTypes.NameIdentifier]));

        if(user == null)
            return Unauthorized();

        return Ok(new {
            AccessToken = tokenHandler.WriteToken(GenerateAccessToken(user)),
            RefreshToken = tokenHandler.WriteToken(GenerateRefreshToken(user)),
        });
    }


    private JwtSecurityToken GenerateToken(User user, bool isAccess)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Name),
            new Claim("is_access", isAccess.ToString()),
            new Claim(ClaimTypes.Role, user.IsAdmin.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: _config["JwtSettings:Issuer"],
            audience: _config["JwtSettings:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(
                isAccess ? _config["JwtSettings:AccessTokenDuration"] : _config["JwtSettings:RefreshTokenDuration"]
            )),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JwtSettings:SecretKey"]!)),
                SecurityAlgorithms.HmacSha256
            )
        );

        return token;
    }

    private JwtSecurityToken GenerateAccessToken(User user) => GenerateToken(user, true);
    
    private JwtSecurityToken GenerateRefreshToken(User user) => GenerateToken(user, false);
}