using System.Linq.Expressions;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto.Models;
using Proyecto.Services;

namespace Proyecto.Controllers;

[Route("api/archivos")]
[ApiController]
[Authorize]
public class ControladorArchivos : ControllerBase
{

    private const long TamanoMaximoBytes = 10 * 1024 * 1024;

    private const int TamanoPaginaPredeterminado = 10;
    private const int TamanoPaginaMaximo = 100;
    private const int LongitudMaximaNombre = 200;
    private const int LongitudMaximaExtension = 20;

    private readonly ContextoProyecto _contexto;
    private readonly AccesoEquipos _acceso;
    private readonly ServicioAlmacenamiento _almacenamiento;

    public ControladorArchivos(ContextoProyecto contexto, ServicioAlmacenamiento almacenamiento, AccesoEquipos acceso)
    {
        _contexto = contexto;
        _acceso = acceso;
        _almacenamiento = almacenamiento;
    }

    [HttpPost]
    public async Task<ActionResult<ArchivoListado>> SubirArchivo(
        [FromForm] ModeloSubirArchivo modelo,
        CancellationToken cancelacion)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var archivo = modelo.Archivo!;

        if (archivo.Length == 0)
            return BadRequest("El archivo está vacío.");

        if (archivo.Length > TamanoMaximoBytes)
            return BadRequest("Los archivos no pueden pesar más de 10 MB.");

        var directorio = await _acceso.DirectoriosEscritura
            .FirstOrDefaultAsync(d => d.Id == modelo.DirectorioId!.Value, cancelacion);

        if (directorio is null)
            return BadRequest("El directorio destino no existe.");

        var nombreOriginal = Path.GetFileName(archivo.FileName);
        var extension = Path.GetExtension(nombreOriginal);
        var nombre = Path.GetFileNameWithoutExtension(nombreOriginal);

        if (string.IsNullOrWhiteSpace(nombre))
            nombre = "archivo";

        if (nombre.Length > LongitudMaximaNombre || extension.Length > LongitudMaximaExtension)
            return BadRequest("El nombre del archivo es demasiado largo.");

        await using var contenido = archivo.OpenReadStream();
        var nombreAlmacenamiento = await _almacenamiento.GuardarAsync(contenido, extension, cancelacion);

        var registro = new Archivo
        {
            Nombre = nombre,
            Extension = extension,
            NombreAlmacenamiento = nombreAlmacenamiento,
            TamanoBytes = archivo.Length,
            TipoContenido = string.IsNullOrWhiteSpace(archivo.ContentType)
                ? "application/octet-stream"
                : archivo.ContentType,
            DirectorioId = directorio.Id,
            PropietarioId = UsuarioActualId(),
        };

        _contexto.Archivos.Add(registro);
        try { await _contexto.SaveChangesAsync(cancelacion); }
        catch { _almacenamiento.Eliminar(nombreAlmacenamiento); throw; }

        return CreatedAtAction(nameof(ObtenerArchivo), new { id = registro.Id }, new ArchivoListado
        {
            Id = registro.Id,
            EquipoId = directorio.EquipoId,
            Nombre = registro.Nombre,
            Extension = registro.Extension,
            NombreCompleto = registro.Nombre + registro.Extension,
            TamanoBytes = registro.TamanoBytes,
            TipoContenido = registro.TipoContenido,
            DirectorioId = registro.DirectorioId,
            NombreDirectorio = directorio.Nombre,
            PropietarioId = registro.PropietarioId,
            Propietario = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
            CreadoEn = registro.CreadoEn,
            ActualizadoEn = registro.ActualizadoEn,
        });
    }

    [HttpGet]
    public async Task<ActionResult<ResultadoPaginado<ArchivoListado>>> ObtenerArchivos(
        [FromQuery] long directorioId,
        string? nombre,
        string? extension,
        long? propietarioId,
        DateOnly? subidoDesde,
        DateOnly? subidoHasta,
        int pagina = 1,
        int tamanoPagina = TamanoPaginaPredeterminado)
    {
        Normalizar(ref pagina, ref tamanoPagina);

        if (!await (await _acceso.DirectoriosLecturaAsync()).AnyAsync(d => d.Id == directorioId))
            return NotFound("El directorio indicado no existe.");

        var consulta = (await _acceso.ArchivosLecturaAsync())
            .AsNoTracking()
            .Where(a => a.DirectorioId == directorioId);

        if (!string.IsNullOrWhiteSpace(nombre))
            consulta = consulta.Where(a => a.Nombre.Contains(nombre));

        if (!string.IsNullOrWhiteSpace(extension))
            consulta = consulta.Where(a => a.Extension.Contains(extension));

        if (propietarioId.HasValue)
            consulta = consulta.Where(a => a.PropietarioId == propietarioId.Value);

        if (subidoDesde.HasValue)
        {
            var desde = subidoDesde.Value.ToDateTime(TimeOnly.MinValue);
            consulta = consulta.Where(a => a.CreadoEn >= desde);
        }

        if (subidoHasta.HasValue)
        {
            var hasta = subidoHasta.Value.ToDateTime(TimeOnly.MaxValue);
            consulta = consulta.Where(a => a.CreadoEn <= hasta);
        }

        var totalRegistros = await consulta.CountAsync();

        var elementos = await consulta
            .OrderBy(a => a.Nombre)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .Select(ProyeccionListado)
            .ToListAsync();

        return Ok(new ResultadoPaginado<ArchivoListado>
        {
            Elementos = elementos,
            Pagina = pagina,
            TamanoPagina = tamanoPagina,
            TotalRegistros = totalRegistros,
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ArchivoListado>> ObtenerArchivo(long id)
    {
        var archivo = await (await _acceso.ArchivosLecturaAsync())
            .AsNoTracking()
            .Where(a => a.Id == id)
            .Select(ProyeccionListado)
            .FirstOrDefaultAsync();

        return archivo is null ? NotFound() : Ok(archivo);
    }

    [HttpGet("{id}/descargar")]
    public async Task<IActionResult> DescargarArchivo(long id)
    {
        var archivo = await (await _acceso.ArchivosLecturaAsync()).AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);

        if (archivo is null)
            return NotFound();

        var rutaCompleta = _almacenamiento.ObtenerRutaCompleta(archivo.NombreAlmacenamiento);

        if (!System.IO.File.Exists(rutaCompleta))
            return NotFound("El contenido del archivo no está disponible en el almacenamiento.");

        return PhysicalFile(rutaCompleta, archivo.TipoContenido, archivo.Nombre + archivo.Extension);
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> RenombrarArchivo(long id, ModeloRenombrar modelo)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var archivo = await _acceso.ArchivosEscritura.FirstOrDefaultAsync(a => a.Id == id);

        if (archivo is null)
            return NotFound();

        if (archivo.PropietarioId != UsuarioActualId() && !EsAdmin())
            return Forbid();

        archivo.Nombre = modelo.Nombre.Trim();
        archivo.ActualizadoEn = DateTime.UtcNow;

        await _contexto.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> EliminarArchivo(long id)
    {
        var archivo = await _acceso.ArchivosEscritura.FirstOrDefaultAsync(a => a.Id == id);

        if (archivo is null)
            return NotFound();

        if (archivo.PropietarioId != UsuarioActualId() && !EsAdmin())
            return Forbid();

        _contexto.Archivos.Remove(archivo);
        await _contexto.SaveChangesAsync();

        _almacenamiento.Eliminar(archivo.NombreAlmacenamiento);

        return NoContent();
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

    private static readonly Expression<Func<Archivo, ArchivoListado>> ProyeccionListado = a => new ArchivoListado
    {
        Id = a.Id,
        EquipoId = a.Directorio.EquipoId,
        Nombre = a.Nombre,
        Extension = a.Extension,
        NombreCompleto = a.Nombre + a.Extension,
        TamanoBytes = a.TamanoBytes,
        TipoContenido = a.TipoContenido,
        DirectorioId = a.DirectorioId,
        NombreDirectorio = a.Directorio.Nombre,
        PropietarioId = a.PropietarioId,
        Propietario = a.Propietario.Nombre,
        CreadoEn = a.CreadoEn,
        ActualizadoEn = a.ActualizadoEn,
    };
}
