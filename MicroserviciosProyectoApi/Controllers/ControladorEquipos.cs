using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto.Models;
using Proyecto.Services;

namespace Proyecto.Controllers;

[ApiController]
[Route("api/equipos")]
[Authorize]
public class ControladorEquipos(ContextoProyecto contexto, AccesoEquipos acceso) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Obtener()
    {
        var consulta = contexto.Equipos.AsNoTracking();
        if (!acceso.EsGlobal) consulta = consulta.Where(e => e.Id == acceso.Actual.EquipoId);
        return Ok(await consulta.OrderBy(e => e.Nombre).Select(e => new
        {
            e.Id, e.Nombre,
        }).ToListAsync());
    }

    [HttpPost]
    [Authorize(Policy = "AdminGlobal")]
    public async Task<IActionResult> Crear(ModeloEquipo modelo)
    {
        var nombre = modelo.Nombre.Trim();
        if (await contexto.Equipos.AnyAsync(e => e.Nombre == nombre)) return Conflict("Ya existe un equipo con ese nombre.");
        var equipo = new Equipo { Nombre = nombre };
        contexto.Equipos.Add(equipo);
        await contexto.SaveChangesAsync();
        return StatusCode(201, new { equipo.Id, equipo.Nombre });
    }

    [HttpPut("{id:long}")]
    [Authorize(Policy = "AdminGlobal")]
    public async Task<IActionResult> Renombrar(long id, ModeloEquipo modelo)
    {
        var equipo = await contexto.Equipos.FindAsync(id);
        if (equipo is null) return NotFound();
        var nombre = modelo.Nombre.Trim();
        if (await contexto.Equipos.AnyAsync(e => e.Id != id && e.Nombre == nombre)) return Conflict("El nombre ya existe.");
        equipo.Nombre = nombre;
        await contexto.SaveChangesAsync();
        return NoContent();
    }
}
