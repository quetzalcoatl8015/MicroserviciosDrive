using System.Linq.Expressions;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto.Models;
using Proyecto.Services;

namespace Proyecto.Controllers;

[Route("api/directorios")]
[ApiController]
[Authorize]
public class ControladorDirectorios : ControllerBase
{
    private const int TamanoPaginaPredeterminado = 10;
    private const int TamanoPaginaMaximo = 100;

    private readonly ContextoProyecto _contexto;
    private readonly AccesoEquipos _acceso;
    private readonly ServicioAlmacenamiento _almacenamiento;

    public ControladorDirectorios(ContextoProyecto contexto, ServicioAlmacenamiento almacenamiento, AccesoEquipos acceso)
    {
        _contexto = contexto;
        _acceso = acceso;
        _almacenamiento = almacenamiento;
    }

    [HttpGet]
    public async Task<ActionResult<ResultadoPaginado<DirectorioListado>>> ObtenerDirectorios(
        long? padreId,
        string? nombre,
        long? equipoId = null,
        int pagina = 1,
        int tamanoPagina = TamanoPaginaPredeterminado)
    {
        Normalizar(ref pagina, ref tamanoPagina);

        var visibles = await _acceso.DirectoriosLecturaAsync();
        if (padreId.HasValue && !await visibles.AnyAsync(d => d.Id == padreId.Value)) return NotFound();
        var equipo = equipoId ?? _acceso.Actual.EquipoId;
        if (!padreId.HasValue && (equipo is null || !_acceso.PuedeEquipo(equipo.Value))) return Forbid();

        var consulta = padreId.HasValue
            ? visibles.AsNoTracking().Where(d => d.PadreId == padreId.Value)
            : visibles.AsNoTracking().Where(d => d.PadreId == null && d.EquipoId == equipo);

        if (!string.IsNullOrWhiteSpace(nombre))
            consulta = consulta.Where(d => d.Nombre.Contains(nombre));

        var totalRegistros = await consulta.CountAsync();

        var elementos = await consulta
            .OrderBy(d => d.Nombre)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .Select(ProyeccionListado)
            .ToListAsync();

        return Ok(new ResultadoPaginado<DirectorioListado>
        {
            Elementos = elementos,
            Pagina = pagina,
            TamanoPagina = tamanoPagina,
            TotalRegistros = totalRegistros,
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DirectorioListado>> ObtenerDirectorio(long id)
    {
        var directorio = await (await _acceso.DirectoriosLecturaAsync())
            .AsNoTracking()
            .Where(d => d.Id == id)
            .Select(ProyeccionListado)
            .FirstOrDefaultAsync();

        return directorio is null ? NotFound() : Ok(directorio);
    }

    [HttpPost]
    public async Task<ActionResult<DirectorioListado>> CrearDirectorio(ModeloCrearDirectorio modelo)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var nombre = modelo.Nombre.Trim();
        var equipoId = modelo.EquipoId ?? _acceso.Actual.EquipoId;
        if (equipoId is null || !_acceso.PuedeEquipo(equipoId.Value)) return Forbid();
        if (modelo.PadreId.HasValue)
        {
            var padre = await _acceso.DirectoriosEscritura.FirstOrDefaultAsync(d => d.Id == modelo.PadreId);
            if (padre is null) return NotFound();
            if (modelo.EquipoId.HasValue && modelo.EquipoId != padre.EquipoId)
                return BadRequest("El directorio padre pertenece a otro equipo.");
            equipoId = padre.EquipoId;
        }
        if (!await _contexto.Equipos.AnyAsync(e => e.Id == equipoId)) return BadRequest("Equipo inválido.");

        if (modelo.PadreId.HasValue
            && !await _contexto.Directorios.AnyAsync(d => d.Id == modelo.PadreId.Value))
        {
            return BadRequest("El directorio padre no existe.");
        }

        if (await ExisteNombreAsync(nombre, modelo.PadreId, equipoId.Value))
            return Conflict("Ya existe un directorio con ese nombre en el mismo nivel.");

        var directorio = new Directorio
        {
            Nombre = nombre,
            EquipoId = equipoId.Value,
            PadreId = modelo.PadreId,
            PropietarioId = UsuarioActualId(),
        };

        _contexto.Directorios.Add(directorio);
        await _contexto.SaveChangesAsync();

        return CreatedAtAction(nameof(ObtenerDirectorio), new { id = directorio.Id }, new DirectorioListado
        {
            Id = directorio.Id,
            EquipoId = directorio.EquipoId,
            Nombre = directorio.Nombre,
            PadreId = directorio.PadreId,
            PropietarioId = directorio.PropietarioId,
            Propietario = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
            CantidadArchivos = 0,
            CantidadSubdirectorios = 0,
            EstaVacio = true,
            CreadoEn = directorio.CreadoEn,
            ActualizadoEn = directorio.ActualizadoEn,
        });
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> RenombrarDirectorio(long id, ModeloRenombrar modelo)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var directorio = await _acceso.DirectoriosEscritura.FirstOrDefaultAsync(d => d.Id == id);

        if (directorio is null)
            return NotFound();

        if (directorio.PropietarioId != UsuarioActualId() && !EsAdmin())
            return Forbid();

        var nombre = modelo.Nombre.Trim();

        if (await ExisteNombreAsync(nombre, directorio.PadreId, directorio.EquipoId, directorio.Id))
            return Conflict("Ya existe un directorio con ese nombre en el mismo nivel.");

        directorio.Nombre = nombre;
        directorio.ActualizadoEn = DateTime.UtcNow;

        await _contexto.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> EliminarDirectorio(long id)
    {
        var directorio = await _acceso.DirectoriosEscritura.FirstOrDefaultAsync(d => d.Id == id);

        if (directorio is null)
            return NotFound();

        if (directorio.PropietarioId != UsuarioActualId() && !EsAdmin())
            return Forbid();

        var cantidadArchivos = await _contexto.Archivos.CountAsync(a => a.DirectorioId == id);
        var cantidadSubdirectorios = await _contexto.Directorios.CountAsync(d => d.PadreId == id);
        var estaVacio = cantidadArchivos == 0 && cantidadSubdirectorios == 0;

        if (!estaVacio && !EsAdmin())
            return BadRequest("Un usuario común solo puede eliminar directorios vacíos.");

        if (estaVacio)
        {
            _contexto.Directorios.Remove(directorio);
            await _contexto.SaveChangesAsync();
            return NoContent();
        }

        await EliminarSubarbolAsync(directorio);

        return NoContent();
    }

    private async Task EliminarSubarbolAsync(Directorio raiz)
    {

        var niveles = new List<List<Directorio>> { new() { raiz } };
        var idsNivelActual = new List<long> { raiz.Id };

        while (true)
        {
            var siguienteNivel = await _contexto.Directorios
                .Where(d => d.PadreId != null && idsNivelActual.Contains(d.PadreId.Value))
                .ToListAsync();

            if (siguienteNivel.Count == 0)
                break;

            niveles.Add(siguienteNivel);
            idsNivelActual = siguienteNivel.Select(d => d.Id).ToList();
        }

        var idsSubarbol = niveles.SelectMany(nivel => nivel).Select(d => d.Id).ToList();

        var archivos = await _contexto.Archivos
            .Where(a => idsSubarbol.Contains(a.DirectorioId))
            .ToListAsync();

        _contexto.Archivos.RemoveRange(archivos);

        for (var nivel = niveles.Count - 1; nivel >= 0; nivel--)
            _contexto.Directorios.RemoveRange(niveles[nivel]);

        await _contexto.SaveChangesAsync();

        foreach (var archivo in archivos)
            _almacenamiento.Eliminar(archivo.NombreAlmacenamiento);
    }

    private Task<bool> ExisteNombreAsync(string nombre, long? padreId, long equipoId, long? idExcluido = null)
    {
        var consulta = padreId.HasValue
            ? _contexto.Directorios.Where(d => d.Nombre == nombre && d.PadreId == padreId.Value)
            : _contexto.Directorios.Where(d => d.Nombre == nombre && d.PadreId == null);

        consulta = consulta.Where(d => d.EquipoId == equipoId);

        if (idExcluido.HasValue)
            consulta = consulta.Where(d => d.Id != idExcluido.Value);

        return consulta.AnyAsync();
    }

    private static void Normalizar(ref int pagina, ref int tamanoPagina)
    {
        if (pagina < 1)
            pagina = 1;

        if (tamanoPagina < 1)
            tamanoPagina = TamanoPaginaPredeterminado;

        if (tamanoPagina > TamanoPaginaMaximo)
            tamanoPagina = TamanoPaginaMaximo;
    }

    private long UsuarioActualId() =>
        long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    private bool EsAdmin() => _acceso.EsAdmin;

    private static readonly Expression<Func<Directorio, DirectorioListado>> ProyeccionListado = d => new DirectorioListado
    {
        Id = d.Id,
        EquipoId = d.EquipoId,
        Nombre = d.Nombre,
        PadreId = d.PadreId,
        PropietarioId = d.PropietarioId,
        Propietario = d.Propietario.Nombre,
        CantidadArchivos = d.Archivos.Count,
        CantidadSubdirectorios = d.Subdirectorios.Count,
        EstaVacio = d.Archivos.Count == 0 && d.Subdirectorios.Count == 0,
        CreadoEn = d.CreadoEn,
        ActualizadoEn = d.ActualizadoEn,
    };
}
