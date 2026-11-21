using System.Linq.Expressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto.Models;
using Proyecto.Services;

namespace Proyecto.Controllers;

[Route("api/usuarios")]
[ApiController]
[Authorize(Policy = "SoloAdmin")]
public class ControladorUsuarios(ContextoProyecto contexto, AccesoEquipos acceso) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ResultadoPaginado<UsuarioListado>>> ObtenerUsuarios(
        string? nombre, string? correo, DateOnly? fechaNacimiento, bool? esAdmin,
        bool? tieneTokenSesion, long? equipoId = null, int pagina = 1, int tamanoPagina = 10)
    {
        pagina = Math.Max(1, pagina);
        tamanoPagina = Math.Clamp(tamanoPagina, 1, 100);
        if (equipoId.HasValue && !acceso.PuedeEquipo(equipoId.Value)) return Forbid();
        var consulta = acceso.Usuarios.AsNoTracking();
        // Las cuentas autoregistradas aún no tienen equipo: se listan junto a las del equipo
        // seleccionado para que el administrador pueda asignarles uno al activarlas.
        if (equipoId.HasValue) consulta = consulta.Where(u => u.EquipoId == equipoId.Value || u.EquipoId == null);
        if (!string.IsNullOrWhiteSpace(nombre)) consulta = consulta.Where(u => u.Nombre.Contains(nombre));
        if (!string.IsNullOrWhiteSpace(correo)) consulta = consulta.Where(u => u.Correo.Contains(correo));
        if (fechaNacimiento.HasValue) consulta = consulta.Where(u => u.FechaNacimiento == fechaNacimiento);
        if (esAdmin.HasValue) consulta = consulta.Where(u => u.EsAdmin == esAdmin);
        if (tieneTokenSesion.HasValue) consulta = consulta.Where(u => (u.TokenSesion != null) == tieneTokenSesion);
        return Ok(new ResultadoPaginado<UsuarioListado>
        {
            Elementos = await consulta.OrderBy(u => u.Id).Skip((pagina - 1) * tamanoPagina)
                .Take(tamanoPagina).Select(Proyeccion).ToListAsync(),
            TotalRegistros = await consulta.CountAsync(), Pagina = pagina, TamanoPagina = tamanoPagina,
        });
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<UsuarioListado>> ObtenerUsuario(long id)
    {
        var usuario = await acceso.Usuarios.AsNoTracking().Where(u => u.Id == id).Select(Proyeccion).FirstOrDefaultAsync();
        return usuario is null ? NotFound() : Ok(usuario);
    }

    [HttpPost]
    public async Task<ActionResult<UsuarioListado>> CrearUsuario(ModeloCrearUsuario modelo)
    {
        var equipoId = modelo.EquipoId ?? acceso.Actual.EquipoId;
        var nivel = Nivel(modelo.EsAdmin, modelo.NivelAdmin);
        if (equipoId is null || !acceso.PuedeEquipo(equipoId.Value) || (!acceso.EsGlobal && nivel == 2)) return Forbid();
        if (!await contexto.Equipos.AnyAsync(e => e.Id == equipoId)) return BadRequest("Equipo inválido.");
        if (await contexto.Usuarios.AnyAsync(u => u.Correo == modelo.Correo)) return Conflict("El correo ya está registrado.");
        var usuario = new Usuario
        {
            Nombre = modelo.Nombre, Correo = modelo.Correo,
            Contrasena = BCrypt.Net.BCrypt.HashPassword(modelo.Contrasena),
            FechaNacimiento = modelo.FechaNacimiento!.Value, EquipoId = equipoId,
            NivelAdmin = nivel, EsAdmin = nivel > 0, EstaActivo = true,
        };
        contexto.Usuarios.Add(usuario);
        await contexto.SaveChangesAsync();
        return CreatedAtAction(nameof(ObtenerUsuario), new { id = usuario.Id },
            await contexto.Usuarios.Where(u => u.Id == usuario.Id).Select(Proyeccion).SingleAsync());
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> EditarUsuario(long id, ModeloEditarUsuario modelo)
    {
        var usuario = await acceso.Usuarios.Include(u => u.TokenSesion).FirstOrDefaultAsync(u => u.Id == id);
        if (usuario is null) return NotFound();
        var equipoId = modelo.EquipoId ?? usuario.EquipoId;
        var nivel = Nivel(modelo.EsAdmin, modelo.NivelAdmin);
        if (!PuedeGestionar(usuario) || equipoId is null || !acceso.PuedeEquipo(equipoId.Value) || (!acceso.EsGlobal && nivel == 2)) return Forbid();
        if (id == acceso.Actual.Id && (nivel != usuario.NivelAdmin || equipoId != usuario.EquipoId))
            return BadRequest("No puedes cambiar tu propio nivel ni equipo.");
        if (usuario.NivelAdmin == 2 && nivel != 2 && await EsUltimoGlobal(usuario))
            return BadRequest("Debe permanecer al menos un administrador de nivel 2 activo.");
        if (!await contexto.Equipos.AnyAsync(e => e.Id == equipoId)) return BadRequest("Equipo inválido.");
        if (await contexto.Usuarios.AnyAsync(u => u.Correo == modelo.Correo && u.Id != id)) return Conflict("El correo ya está registrado.");
        var revocar = nivel != usuario.NivelAdmin || equipoId != usuario.EquipoId || !string.IsNullOrEmpty(modelo.Contrasena);
        usuario.Nombre = modelo.Nombre;
        usuario.Correo = modelo.Correo;
        usuario.FechaNacimiento = modelo.FechaNacimiento!.Value;
        usuario.EquipoId = equipoId;
        usuario.NivelAdmin = nivel;
        usuario.EsAdmin = nivel > 0;
        usuario.ActualizadoEn = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(modelo.Contrasena)) usuario.Contrasena = BCrypt.Net.BCrypt.HashPassword(modelo.Contrasena);
        if (revocar && usuario.TokenSesion is not null) contexto.TokensSesion.Remove(usuario.TokenSesion);
        await contexto.SaveChangesAsync();
        return NoContent();
    }

    [HttpPatch("{id:long}/estado")]
    public async Task<IActionResult> CambiarEstadoUsuario(long id, ModeloCambiarEstadoUsuario modelo)
    {
        var usuario = await acceso.Usuarios.Include(u => u.TokenSesion).FirstOrDefaultAsync(u => u.Id == id);
        if (usuario is null) return NotFound();
        if (!PuedeGestionar(usuario)) return Forbid();
        if (!modelo.EstaActivo && (id == acceso.Actual.Id || await EsUltimoGlobal(usuario)))
            return BadRequest("No puedes desactivarte ni desactivar al último administrador global activo.");
        if (modelo.EstaActivo)
        {
            // Activar es dar acceso: la cuenta debe quedar dentro de un equipo, y solo un
            // administrador puede decidir cuál (nivel 1 únicamente el suyo).
            var equipoId = modelo.EquipoId ?? usuario.EquipoId;
            if (equipoId is null) return BadRequest("Asigna un equipo antes de activar la cuenta.");
            if (!acceso.PuedeEquipo(equipoId.Value)) return Forbid();
            if (!await contexto.Equipos.AnyAsync(e => e.Id == equipoId.Value)) return BadRequest("Equipo inválido.");
            usuario.EquipoId = equipoId;
        }
        usuario.EstaActivo = modelo.EstaActivo;
        usuario.ActualizadoEn = DateTime.UtcNow;
        if (!modelo.EstaActivo && usuario.TokenSesion is not null) contexto.TokensSesion.Remove(usuario.TokenSesion);
        await contexto.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:long}/token-sesion")]
    public async Task<IActionResult> RevocarTokenSesion(long id)
    {
        var usuario = await acceso.Usuarios.Include(u => u.TokenSesion).FirstOrDefaultAsync(u => u.Id == id);
        if (usuario is null) return NotFound();
        if (!PuedeGestionar(usuario)) return Forbid();
        if (id == acceso.Actual.Id) return BadRequest("No puedes revocar tu propio token de sesión.");
        if (usuario.TokenSesion is null) return NotFound();
        contexto.TokensSesion.Remove(usuario.TokenSesion);
        await contexto.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> EliminarUsuario(long id)
    {
        var usuario = await acceso.Usuarios.FirstOrDefaultAsync(u => u.Id == id);
        if (usuario is null) return NotFound();
        if (!PuedeGestionar(usuario)) return Forbid();
        if (id == acceso.Actual.Id || await EsUltimoGlobal(usuario)) return BadRequest("No puedes eliminarte ni eliminar al último administrador global activo.");
        // Eliminar una cuenta no debe borrar carpetas que contienen trabajo de otros usuarios.
        if (await contexto.Directorios.AnyAsync(d => d.PropietarioId == id) || await contexto.Archivos.AnyAsync(a => a.PropietarioId == id))
            return Conflict("El usuario tiene contenido. Desactívalo o elimina primero su contenido.");
        contexto.Usuarios.Remove(usuario);
        await contexto.SaveChangesAsync();
        return NoContent();
    }

    private bool PuedeGestionar(Usuario usuario) => acceso.EsGlobal || usuario.NivelAdmin < 2;
    private static int Nivel(bool esAdmin, int nivel) => nivel > 0 ? nivel : esAdmin ? 1 : 0;
    private async Task<bool> EsUltimoGlobal(Usuario usuario) => usuario.EstaActivo && usuario.NivelAdmin == 2
        && !await contexto.Usuarios.AnyAsync(u => u.Id != usuario.Id && u.EstaActivo && u.EsAdmin && u.NivelAdmin == 2);

    private static readonly Expression<Func<Usuario, UsuarioListado>> Proyeccion = u => new UsuarioListado
    {
        Id = u.Id, Nombre = u.Nombre, Correo = u.Correo, FechaNacimiento = u.FechaNacimiento,
        EsAdmin = u.EsAdmin, NivelAdmin = u.NivelAdmin, EquipoId = u.EquipoId, Equipo = u.Equipo == null ? null : u.Equipo.Nombre,
        EstaActivo = u.EstaActivo, TieneTokenSesion = u.TokenSesion != null,
        CreadoEn = u.CreadoEn, ActualizadoEn = u.ActualizadoEn,
    };
}
