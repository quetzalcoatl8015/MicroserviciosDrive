using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto.Models;
using Proyecto.Services;

namespace Proyecto.Controllers;

[Route("api/autenticacion")]
[ApiController]
public class ControladorAutenticacion : ControllerBase
{
    private readonly ContextoProyecto _contexto;
    private readonly ServicioJwt _servicioJwt;

    public ControladorAutenticacion(ContextoProyecto contexto, ServicioJwt servicioJwt)
    {
        _contexto = contexto;
        _servicioJwt = servicioJwt;
    }

    [HttpPost("registro")]
    public async Task<IActionResult> Registrar(ModeloRegistro modelo)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (await _contexto.Usuarios.AnyAsync(u => u.Correo == modelo.Correo))
            return Conflict("Ya existe un usuario registrado con ese correo.");

        var usuario = new Usuario
        {
            Nombre = modelo.Nombre,
            Correo = modelo.Correo,
            Contrasena = BCrypt.Net.BCrypt.HashPassword(modelo.Contrasena),
            FechaNacimiento = modelo.FechaNacimiento!.Value,
            EsAdmin = false,
            NivelAdmin = 0,
            EquipoId = null,
            EstaActivo = false,
        };

        _contexto.Usuarios.Add(usuario);
        await _contexto.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, new
        {
            usuario.Id,
            usuario.Nombre,
            usuario.Correo,
            usuario.EstaActivo,
        });
    }

    [HttpPost("iniciar-sesion")]
    public async Task<IActionResult> IniciarSesion(CredencialesAcceso credenciales)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var usuario = await _contexto.Usuarios.FirstOrDefaultAsync(u => u.Correo == credenciales.Correo);

        if (usuario is null || !BCrypt.Net.BCrypt.Verify(credenciales.Contrasena, usuario.Contrasena))
            return Unauthorized("Credenciales inválidas.");

        if (!usuario.EstaActivo)
            return Unauthorized("El usuario está desactivado.");

        return Ok(await EmitirTokensAsync(usuario));
    }

    [HttpPost("renovar")]
    public async Task<IActionResult> Renovar(SolicitudTokenRefresco solicitud)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var hashToken = CalcularHashToken(solicitud.TokenRefresco);

        var guardado = await _contexto.TokensSesion
            .Include(t => t.Usuario)
            .FirstOrDefaultAsync(t => t.Token == hashToken);

        if (guardado is null || guardado.ExpiraEn <= DateTime.UtcNow || !guardado.Usuario.EstaActivo)
            return Unauthorized("El token de refresco no es válido.");

        return Ok(await EmitirTokensAsync(guardado.Usuario));
    }

    [Authorize]
    [HttpGet("yo")]
    public async Task<IActionResult> Yo()
    {
        var usuario = (Usuario)HttpContext.Items["UsuarioActual"]!;
        var equipo = await _contexto.Equipos.Where(e => e.Id == usuario.EquipoId).Select(e => e.Nombre).FirstOrDefaultAsync();
        return Ok(new { usuario.Id, usuario.Nombre, usuario.Correo, usuario.EsAdmin, usuario.NivelAdmin, usuario.EquipoId, Equipo = equipo });
    }

    [HttpPost("cerrar-sesion")]
    [Authorize]
    public async Task<IActionResult> CerrarSesion()
    {
        if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
            return Unauthorized();

        var guardado = await _contexto.TokensSesion.FirstOrDefaultAsync(t => t.UsuarioId == usuarioId);

        if (guardado is not null)
        {
            _contexto.TokensSesion.Remove(guardado);
            await _contexto.SaveChangesAsync();
        }

        return NoContent();
    }

    private async Task<TokensAcceso> EmitirTokensAsync(Usuario usuario)
    {
        var tokenRefresco = _servicioJwt.CrearTokenRefresco(usuario);
        var expiraEn = DateTime.UtcNow.AddMinutes(_servicioJwt.MinutosTokenRefresco);

        var hashToken = CalcularHashToken(tokenRefresco);

        var guardado = await _contexto.TokensSesion.FirstOrDefaultAsync(t => t.UsuarioId == usuario.Id);

        if (guardado is null)
        {
            _contexto.TokensSesion.Add(new TokenSesion
            {
                UsuarioId = usuario.Id,
                Token = hashToken,
                ExpiraEn = expiraEn,
            });
        }
        else
        {
            guardado.Token = hashToken;
            guardado.ExpiraEn = expiraEn;
            guardado.CreadoEn = DateTime.UtcNow;
        }

        await _contexto.SaveChangesAsync();

        return new TokensAcceso(_servicioJwt.CrearTokenAcceso(usuario), tokenRefresco);
    }

    private static string CalcularHashToken(string token)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
