using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto.Models;
using Proyecto.Services;

namespace Proyecto.Controllers;

/// <summary>
/// Módulo de autenticación (módulo 1 del PDF): registro, inicio de sesión,
/// renovación de la sesión y cierre de sesión.
/// </summary>
/// <remarks>
/// La ruta base se declara explícitamente en español (<c>api/autenticacion</c>) en lugar
/// de usar la plantilla <c>api/[controller]</c>, porque esa plantilla generaría una ruta
/// con el nombre de la clase y quedaría mezclada con inglés.
/// </remarks>
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

    /// <summary>
    /// Autoregistro de un usuario. Queda desactivado y como usuario común hasta que
    /// un administrador lo active.
    /// </summary>
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

    /// <summary>
    /// Inicia sesión y entrega un par de tokens (de acceso y de refresco).
    /// </summary>
    [HttpPost("iniciar-sesion")]
    public async Task<IActionResult> IniciarSesion(CredencialesAcceso credenciales)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var usuario = await _contexto.Usuarios.FirstOrDefaultAsync(u => u.Correo == credenciales.Correo);

        // BCrypt.Verify compara la contraseña recibida contra el hash guardado.
        if (usuario is null || !BCrypt.Net.BCrypt.Verify(credenciales.Contrasena, usuario.Contrasena))
            return Unauthorized("Credenciales inválidas.");

        if (!usuario.EstaActivo)
            return Unauthorized("El usuario está desactivado.");

        return Ok(await EmitirTokensAsync(usuario));
    }

    /// <summary>
    /// Obtiene un nuevo par de tokens a partir de un token de refresco vigente.
    /// El token de refresco anterior queda invalidado (rotación obligatoria).
    /// </summary>
    [HttpPost("renovar")]
    public async Task<IActionResult> Renovar(SolicitudTokenRefresco solicitud)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        // En la base sólo vive el hash del token, así que se busca por su hash.
        var hashToken = CalcularHashToken(solicitud.TokenRefresco);

        var guardado = await _contexto.TokensSesion
            .Include(t => t.Usuario)
            .FirstOrDefaultAsync(t => t.Token == hashToken);

        if (guardado is null || guardado.ExpiraEn <= DateTime.UtcNow || !guardado.Usuario.EstaActivo)
            return Unauthorized("El token de refresco no es válido.");

        return Ok(await EmitirTokensAsync(guardado.Usuario));
    }

    /// <summary>
    /// Cierra la sesión del usuario autenticado revocando su token de refresco.
    /// </summary>
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

    /// <summary>
    /// Emite un nuevo par de tokens y deja asignado el token de refresco al usuario,
    /// invalidando el que tuviera antes.
    /// </summary>
    private async Task<TokensAcceso> EmitirTokensAsync(Usuario usuario)
    {
        var tokenRefresco = _servicioJwt.CrearTokenRefresco(usuario);
        var expiraEn = DateTime.UtcNow.AddMinutes(_servicioJwt.MinutosTokenRefresco);

        // Se guarda el hash, no el token. Si alguien leyera la tabla no podría
        // reutilizar el token de refresco, y la columna queda de tamaño fijo.
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

    /// <summary>
    /// Calcula el hash SHA-256 (en hexadecimal) de un token de refresco.
    /// Se usa para no guardar el token en claro: la base almacena 64 caracteres
    /// fijos en lugar del JWT completo, que puede superar los 512 caracteres.
    /// </summary>
    private static string CalcularHashToken(string token)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
