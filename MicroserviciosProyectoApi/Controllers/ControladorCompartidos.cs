using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto.Models;
using Proyecto.Services;

namespace Proyecto.Controllers;

[ApiController]
[Route("api/compartidos")]
[Authorize]
public class ControladorCompartidos(ContextoProyecto contexto, AccesoEquipos acceso) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Obtener(long? equipoId = null)
    {
        var equipo = equipoId ?? acceso.Actual.EquipoId;
        if (!acceso.PuedeEquipo(equipo)) return Forbid();
        return Ok(await contexto.ContenidosCompartidos.AsNoTracking()
            .Where(c => c.EquipoDestinoId == equipo).OrderBy(c => c.Id).Select(c => new
            {
                c.Id, c.EquipoDestinoId, c.ArchivoId, c.DirectorioId, c.CreadoEn,
                Nombre = c.ArchivoId != null ? c.Archivo!.Nombre + c.Archivo.Extension : c.Directorio!.Nombre,
                EquipoOrigen = c.ArchivoId != null ? c.Archivo!.Directorio.Equipo.Nombre : c.Directorio!.Equipo.Nombre,
                SoloLectura = true,
            }).ToListAsync());
    }

    [HttpGet("recurso")]
    [Authorize(Policy = "AdminGlobal")]
    public async Task<IActionResult> PorRecurso(long? archivoId, long? directorioId)
    {
        if (archivoId.HasValue == directorioId.HasValue) return BadRequest("Indica un solo recurso.");
        return Ok(await contexto.ContenidosCompartidos.AsNoTracking()
            .Where(c => archivoId.HasValue ? c.ArchivoId == archivoId : c.DirectorioId == directorioId)
            .Select(c => new { c.Id, c.EquipoDestinoId, EquipoDestino = c.EquipoDestino.Nombre }).ToListAsync());
    }

    [HttpPost]
    [Authorize(Policy = "AdminGlobal")]
    public async Task<IActionResult> Compartir(ModeloCompartir modelo)
    {
        if (!await contexto.Equipos.AnyAsync(e => e.Id == modelo.EquipoDestinoId)) return BadRequest("Equipo destino inválido.");
        long? origen = modelo.ArchivoId.HasValue
            ? await contexto.Archivos.Where(a => a.Id == modelo.ArchivoId).Select(a => (long?)a.Directorio.EquipoId).FirstOrDefaultAsync()
            : await contexto.Directorios.Where(d => d.Id == modelo.DirectorioId).Select(d => (long?)d.EquipoId).FirstOrDefaultAsync();
        if (!origen.HasValue) return NotFound();
        if (origen == modelo.EquipoDestinoId) return BadRequest("El contenido ya pertenece a ese equipo.");
        if (await contexto.ContenidosCompartidos.AnyAsync(c => c.EquipoDestinoId == modelo.EquipoDestinoId
            && c.ArchivoId == modelo.ArchivoId && c.DirectorioId == modelo.DirectorioId))
            return Conflict("El recurso ya está compartido con ese equipo.");
        var registro = new ContenidoCompartido
        {
            EquipoDestinoId = modelo.EquipoDestinoId, ArchivoId = modelo.ArchivoId, DirectorioId = modelo.DirectorioId,
        };
        contexto.ContenidosCompartidos.Add(registro);
        await contexto.SaveChangesAsync();
        return StatusCode(201, new { registro.Id });
    }

    [HttpDelete("{id:long}")]
    [Authorize(Policy = "AdminGlobal")]
    public async Task<IActionResult> Revocar(long id)
    {
        var registro = await contexto.ContenidosCompartidos.FindAsync(id);
        if (registro is null) return NotFound();
        contexto.ContenidosCompartidos.Remove(registro);
        await contexto.SaveChangesAsync();
        return NoContent();
    }
}
