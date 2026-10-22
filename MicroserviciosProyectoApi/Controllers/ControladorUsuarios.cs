using System.Linq.Expressions;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto.Models;

namespace Proyecto.Controllers;

/// <summary>
/// Módulo de usuarios (módulo 2 del PDF). Solo accesible para administradores.
/// </summary>
[Route("api/usuarios")]
[ApiController]
[Authorize(Policy = "SoloAdmin")]
public class ControladorUsuarios : ControllerBase
{
    private const int TamanoPaginaPredeterminado = 10;
    private const int TamanoPaginaMaximo = 100;

    private readonly ContextoProyecto _contexto;

    public ControladorUsuarios(ContextoProyecto contexto)
    {
        _contexto = contexto;
    }

    /// <summary>
    /// Consulta la lista paginada de usuarios registrados.
    /// </summary>
    /// <param name="nombre">Subcadena del nombre</param>
    /// <param name="correo">Subcadena del correo</param>
    /// <param name="fechaNacimiento">Fecha de nacimiento exacta</param>
    /// <param name="esAdmin">Si es administrador o no</param>
    /// <param name="tieneTokenSesion">Si tiene token de refresco asignado o no</param>
    /// <param name="pagina">Número de página (inicia en 1)</param>
    /// <param name="tamanoPagina">Tamaño de página (máximo 100)</param>
    [HttpGet]
    public async Task<ActionResult<ResultadoPaginado<UsuarioListado>>> ObtenerUsuarios(
        string? nombre,
        string? correo,
        DateOnly? fechaNacimiento,
        bool? esAdmin,
        bool? tieneTokenSesion,
        int pagina = 1,
        int tamanoPagina = TamanoPaginaPredeterminado)
    {
        Normalizar(ref pagina, ref tamanoPagina);

        var consulta = _contexto.Usuarios.AsNoTracking();

        // Los filtros de texto son por subcadena: "ju" encuentra "Juan".
        if (!string.IsNullOrWhiteSpace(nombre))
            consulta = consulta.Where(u => u.Nombre.Contains(nombre));

        if (!string.IsNullOrWhiteSpace(correo))
            consulta = consulta.Where(u => u.Correo.Contains(correo));

        if (fechaNacimiento.HasValue)
            consulta = consulta.Where(u => u.FechaNacimiento == fechaNacimiento.Value);

        if (esAdmin.HasValue)
            consulta = consulta.Where(u => u.EsAdmin == esAdmin.Value);

        if (tieneTokenSesion.HasValue)
            consulta = consulta.Where(u => (u.TokenSesion != null) == tieneTokenSesion.Value);

        var totalRegistros = await consulta.CountAsync();

        var elementos = await consulta
            .OrderBy(u => u.Id)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .Select(ProyeccionListado)
            .ToListAsync();

        return Ok(new ResultadoPaginado<UsuarioListado>
        {
            Elementos = elementos,
            Pagina = pagina,
            TamanoPagina = tamanoPagina,
            TotalRegistros = totalRegistros,
        });
    }

    /// <summary>
    /// Consulta un usuario por su identificador.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<UsuarioListado>> ObtenerUsuario(long id)
    {
        var usuario = await _contexto.Usuarios
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(ProyeccionListado)
            .FirstOrDefaultAsync();

        return usuario is null ? NotFound() : Ok(usuario);
    }

    /// <summary>
    /// Registra un nuevo usuario. Los usuarios creados por un administrador
    /// quedan activos desde el inicio.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<UsuarioListado>> CrearUsuario(ModeloCrearUsuario modelo)
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
            EsAdmin = modelo.EsAdmin,
            // A diferencia del autoregistro, un usuario creado por un administrador nace activo.
            EstaActivo = true,
        };

        _contexto.Usuarios.Add(usuario);
        await _contexto.SaveChangesAsync();

        return CreatedAtAction(nameof(ObtenerUsuario), new { id = usuario.Id }, new UsuarioListado
        {
            Id = usuario.Id,
            Nombre = usuario.Nombre,
            Correo = usuario.Correo,
            FechaNacimiento = usuario.FechaNacimiento,
            EsAdmin = usuario.EsAdmin,
            EstaActivo = usuario.EstaActivo,
            TieneTokenSesion = false,
            CreadoEn = usuario.CreadoEn,
            ActualizadoEn = usuario.ActualizadoEn,
        });
    }

    /// <summary>
    /// Edita la información de un usuario. Si se cambia la contraseña,
    /// su token de refresco queda revocado.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> EditarUsuario(long id, ModeloEditarUsuario modelo)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var usuario = await _contexto.Usuarios
            .Include(u => u.TokenSesion)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (usuario is null)
            return NotFound();

        if (await _contexto.Usuarios.AnyAsync(u => u.Correo == modelo.Correo && u.Id != id))
            return Conflict("Ya existe un usuario registrado con ese correo.");

        // Un usuario administrador no puede revocarse a sí mismo el rol de administrador.
        if (usuario.Id == UsuarioActualId() && usuario.EsAdmin && !modelo.EsAdmin)
            return BadRequest("No puedes revocar tu propio rol de administrador.");

        usuario.Nombre = modelo.Nombre;
        usuario.Correo = modelo.Correo;
        usuario.FechaNacimiento = modelo.FechaNacimiento!.Value;
        usuario.EsAdmin = modelo.EsAdmin;
        usuario.ActualizadoEn = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(modelo.Contrasena))
        {
            usuario.Contrasena = BCrypt.Net.BCrypt.HashPassword(modelo.Contrasena);

            // Cambiar la contraseña cierra las sesiones abiertas de ese usuario.
            if (usuario.TokenSesion is not null)
                _contexto.TokensSesion.Remove(usuario.TokenSesion);
        }

        await _contexto.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Activa o desactiva un usuario. Al desactivarlo se revoca su token de refresco.
    /// </summary>
    [HttpPatch("{id}/estado")]
    public async Task<IActionResult> CambiarEstadoUsuario(long id, ModeloCambiarEstadoUsuario modelo)
    {
        var usuario = await _contexto.Usuarios
            .Include(u => u.TokenSesion)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (usuario is null)
            return NotFound();

        // Un usuario administrador no puede desactivarse a sí mismo.
        if (usuario.Id == UsuarioActualId() && !modelo.EstaActivo)
            return BadRequest("No puedes desactivar tu propio usuario.");

        usuario.EstaActivo = modelo.EstaActivo;
        usuario.ActualizadoEn = DateTime.UtcNow;

        if (!modelo.EstaActivo && usuario.TokenSesion is not null)
            _contexto.TokensSesion.Remove(usuario.TokenSesion);

        await _contexto.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Revoca el token de refresco de un usuario (requerimiento 2.5).
    /// </summary>
    [HttpDelete("{id}/token-sesion")]
    public async Task<IActionResult> RevocarTokenSesion(long id)
    {
        // Un usuario no puede revocarse su propio token de refresco.
        if (id == UsuarioActualId())
            return BadRequest("No puedes revocar tu propio token de sesión.");

        var guardado = await _contexto.TokensSesion.FirstOrDefaultAsync(t => t.UsuarioId == id);

        if (guardado is null)
            return NotFound();

        _contexto.TokensSesion.Remove(guardado);
        await _contexto.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Elimina un usuario junto con su token de refresco.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> EliminarUsuario(long id)
    {
        var usuario = await _contexto.Usuarios.FindAsync(id);

        if (usuario is null)
            return NotFound();

        _contexto.Usuarios.Remove(usuario);
        await _contexto.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>Ajusta la paginación a valores válidos antes de consultar.</summary>
    private static void Normalizar(ref int pagina, ref int tamanoPagina)
    {
        if (pagina < 1)
            pagina = 1;

        if (tamanoPagina < 1)
            tamanoPagina = TamanoPaginaPredeterminado;

        if (tamanoPagina > TamanoPaginaMaximo)
            tamanoPagina = TamanoPaginaMaximo;
    }

    /// <summary>Identificador del usuario que está haciendo la petición.</summary>
    private long UsuarioActualId() =>
        long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    /// <summary>Proyección que nunca expone la contraseña del usuario.</summary>
    private static readonly Expression<Func<Usuario, UsuarioListado>> ProyeccionListado = u => new UsuarioListado
    {
        Id = u.Id,
        Nombre = u.Nombre,
        Correo = u.Correo,
        FechaNacimiento = u.FechaNacimiento,
        EsAdmin = u.EsAdmin,
        EstaActivo = u.EstaActivo,
        TieneTokenSesion = u.TokenSesion != null,
        CreadoEn = u.CreadoEn,
        ActualizadoEn = u.ActualizadoEn,
    };
}
