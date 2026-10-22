using System.Linq.Expressions;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto.Models;
using Proyecto.Services;

namespace Proyecto.Controllers;

/// <summary>
/// Módulo de directorios (requerimientos 3.1, 3.3, 3.4 y 3.5).
/// Cualquier usuario autenticado puede consultar y crear directorios; renombrar y eliminar
/// quedan reservados al creador del directorio o a un administrador.
/// </summary>
/// <remarks>
/// Antes este archivo necesitaba el alias <c>using Directory = Proyecto.Models.Directory;</c>
/// porque el nombre <c>Directory</c> era ambiguo con <c>System.IO.Directory</c>. Al llamarse
/// <c>Directorio</c> ya no hay ambigüedad y el alias dejó de ser necesario.
/// </remarks>
[Route("api/directorios")]
[ApiController]
[Authorize]
public class ControladorDirectorios : ControllerBase
{
    private const int TamanoPaginaPredeterminado = 10;
    private const int TamanoPaginaMaximo = 100;

    private readonly ContextoProyecto _contexto;
    private readonly ServicioAlmacenamiento _almacenamiento;

    public ControladorDirectorios(ContextoProyecto contexto, ServicioAlmacenamiento almacenamiento)
    {
        _contexto = contexto;
        _almacenamiento = almacenamiento;
    }

    /// <summary>
    /// Lista paginada de directorios de un nivel del repositorio.
    /// </summary>
    /// <param name="padreId">Directorio padre. Omitido o nulo devuelve los directorios de la raíz.</param>
    /// <param name="nombre">Subcadena del nombre del directorio</param>
    /// <param name="pagina">Número de página (inicia en 1)</param>
    /// <param name="tamanoPagina">Tamaño de página (máximo 100)</param>
    [HttpGet]
    public async Task<ActionResult<ResultadoPaginado<DirectorioListado>>> ObtenerDirectorios(
        long? padreId,
        string? nombre,
        int pagina = 1,
        int tamanoPagina = TamanoPaginaPredeterminado)
    {
        Normalizar(ref pagina, ref tamanoPagina);

        // Se filtra por nivel. La comparación con el padre se hace en dos ramas explícitas
        // para que EF genere "IS NULL" cuando se piden los directorios de la raíz.
        var consulta = padreId.HasValue
            ? _contexto.Directorios.AsNoTracking().Where(d => d.PadreId == padreId.Value)
            : _contexto.Directorios.AsNoTracking().Where(d => d.PadreId == null);

        // Los filtros de texto son por subcadena: "do" encuentra "Documentos".
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

    /// <summary>
    /// Consulta un directorio por su identificador.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<DirectorioListado>> ObtenerDirectorio(long id)
    {
        var directorio = await _contexto.Directorios
            .AsNoTracking()
            .Where(d => d.Id == id)
            .Select(ProyeccionListado)
            .FirstOrDefaultAsync();

        return directorio is null ? NotFound() : Ok(directorio);
    }

    /// <summary>
    /// Crea un directorio (requerimiento 3.1). Solo se necesita el nombre y, opcionalmente,
    /// el directorio padre donde se colocará.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<DirectorioListado>> CrearDirectorio(ModeloCrearDirectorio modelo)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var nombre = modelo.Nombre.Trim();

        // El padre, si se indica, debe existir: evita crear niveles colgados.
        if (modelo.PadreId.HasValue
            && !await _contexto.Directorios.AnyAsync(d => d.Id == modelo.PadreId.Value))
        {
            return BadRequest("El directorio padre no existe.");
        }

        if (await ExisteNombreAsync(nombre, modelo.PadreId))
            return Conflict("Ya existe un directorio con ese nombre en el mismo nivel.");

        var directorio = new Directorio
        {
            Nombre = nombre,
            PadreId = modelo.PadreId,
            PropietarioId = UsuarioActualId(),
        };

        _contexto.Directorios.Add(directorio);
        await _contexto.SaveChangesAsync();

        return CreatedAtAction(nameof(ObtenerDirectorio), new { id = directorio.Id }, new DirectorioListado
        {
            Id = directorio.Id,
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

    /// <summary>
    /// Renombra un directorio (requerimiento 3.4). Solo el creador o un administrador.
    /// </summary>
    [HttpPatch("{id}")]
    public async Task<IActionResult> RenombrarDirectorio(long id, ModeloRenombrar modelo)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var directorio = await _contexto.Directorios.FirstOrDefaultAsync(d => d.Id == id);

        if (directorio is null)
            return NotFound();

        // Solo el usuario que lo creó o un administrador pueden editarlo.
        if (directorio.PropietarioId != UsuarioActualId() && !EsAdmin())
            return Forbid();

        var nombre = modelo.Nombre.Trim();

        if (await ExisteNombreAsync(nombre, directorio.PadreId, directorio.Id))
            return Conflict("Ya existe un directorio con ese nombre en el mismo nivel.");

        directorio.Nombre = nombre;
        directorio.ActualizadoEn = DateTime.UtcNow;

        await _contexto.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Elimina un directorio (requerimiento 3.3).
    /// Un usuario común solo puede eliminarlo si está vacío; un administrador puede
    /// eliminarlo con todo su contenido.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> EliminarDirectorio(long id)
    {
        var directorio = await _contexto.Directorios.FirstOrDefaultAsync(d => d.Id == id);

        if (directorio is null)
            return NotFound();

        // Solo el usuario que lo creó o un administrador pueden eliminarlo.
        if (directorio.PropietarioId != UsuarioActualId() && !EsAdmin())
            return Forbid();

        var cantidadArchivos = await _contexto.Archivos.CountAsync(a => a.DirectorioId == id);
        var cantidadSubdirectorios = await _contexto.Directorios.CountAsync(d => d.PadreId == id);
        var estaVacio = cantidadArchivos == 0 && cantidadSubdirectorios == 0;

        // Un usuario común no puede borrar directorios con contenido.
        if (!estaVacio && !EsAdmin())
            return BadRequest("Un usuario común solo puede eliminar directorios vacíos.");

        if (estaVacio)
        {
            _contexto.Directorios.Remove(directorio);
            await _contexto.SaveChangesAsync();
            return NoContent();
        }

        // Administrador sobre un directorio con contenido: se elimina el subárbol completo.
        await EliminarSubarbolAsync(directorio);

        return NoContent();
    }

    /// <summary>
    /// Elimina un directorio junto con todos sus subdirectorios y archivos, tanto los
    /// registros de la base de datos como el contenido físico del almacenamiento.
    /// </summary>
    private async Task EliminarSubarbolAsync(Directorio raiz)
    {
        // Se recorre el árbol por niveles (anchura) guardando cada nivel por separado.
        // Guardar los niveles permite después borrar de abajo hacia arriba, que es el único
        // orden que no viola la llave foránea autorreferente configurada como Restrict.
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

        // Primero los archivos, después los directorios del más profundo al menos profundo.
        _contexto.Archivos.RemoveRange(archivos);

        for (var nivel = niveles.Count - 1; nivel >= 0; nivel--)
            _contexto.Directorios.RemoveRange(niveles[nivel]);

        await _contexto.SaveChangesAsync();

        // El borrado físico se hace después de guardar los cambios: si la operación en la
        // base de datos falla, los archivos siguen en disco en lugar de quedar registros
        // apuntando a archivos que ya no existen.
        foreach (var archivo in archivos)
            _almacenamiento.Eliminar(archivo.NombreAlmacenamiento);
    }

    /// <summary>
    /// Indica si ya existe un directorio con ese nombre en el mismo nivel.
    /// Se consulta explícitamente porque MySQL permite varios NULL en un índice único, por lo
    /// que el índice de la base de datos no puede impedir nombres repetidos en la raíz.
    /// </summary>
    private Task<bool> ExisteNombreAsync(string nombre, long? padreId, long? idExcluido = null)
    {
        var consulta = padreId.HasValue
            ? _contexto.Directorios.Where(d => d.Nombre == nombre && d.PadreId == padreId.Value)
            : _contexto.Directorios.Where(d => d.Nombre == nombre && d.PadreId == null);

        // Al renombrar, el propio directorio no debe contar como duplicado.
        if (idExcluido.HasValue)
            consulta = consulta.Where(d => d.Id != idExcluido.Value);

        return consulta.AnyAsync();
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

    /// <summary>Indica si quien hace la petición es administrador (claim de rol = "True").</summary>
    private bool EsAdmin() => User.FindFirstValue(ClaimTypes.Role) == "True";

    /// <summary>
    /// Proyección reutilizada por las consultas. Los conteos permiten al cliente saber si el
    /// directorio está vacío sin pedir información adicional.
    /// </summary>
    private static readonly Expression<Func<Directorio, DirectorioListado>> ProyeccionListado = d => new DirectorioListado
    {
        Id = d.Id,
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
