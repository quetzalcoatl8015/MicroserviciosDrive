using System.Linq.Expressions;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto.Models;
using Proyecto.Services;

namespace Proyecto.Controllers;

/// <summary>
/// Módulo de archivos (requerimientos 3.2 a 3.6).
/// Cualquier usuario autenticado puede consultar y descargar; subir lo puede hacer cualquiera,
/// pero renombrar y eliminar quedan reservados a quien subió el archivo o a un administrador.
/// </summary>
[Route("api/archivos")]
[ApiController]
[Authorize]
public class ControladorArchivos : ControllerBase
{
    /// <summary>Límite de tamaño por archivo: 10 MB (requerimiento 3.2).</summary>
    private const long TamanoMaximoBytes = 10 * 1024 * 1024;

    private const int TamanoPaginaPredeterminado = 10;
    private const int TamanoPaginaMaximo = 100;
    private const int LongitudMaximaNombre = 200;
    private const int LongitudMaximaExtension = 20;

    private readonly ContextoProyecto _contexto;
    private readonly ServicioAlmacenamiento _almacenamiento;

    public ControladorArchivos(ContextoProyecto contexto, ServicioAlmacenamiento almacenamiento)
    {
        _contexto = contexto;
        _almacenamiento = almacenamiento;
    }

    /// <summary>
    /// Sube un archivo al repositorio (requerimiento 3.2). El envío es multipart/form-data
    /// con la parte "Archivo" para el contenido y "DirectorioId" para el directorio destino.
    /// Los archivos solo pueden subirse dentro de un directorio, nunca en la raíz.
    /// </summary>
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

        // Tope de 10 MB. Cualquier tipo de archivo se acepta mientras respete el tamaño.
        if (archivo.Length > TamanoMaximoBytes)
            return BadRequest("Los archivos no pueden pesar más de 10 MB.");

        // El destino debe existir: así se garantiza que ningún archivo quede en la raíz.
        var directorio = await _contexto.Directorios
            .FirstOrDefaultAsync(d => d.Id == modelo.DirectorioId!.Value, cancelacion);

        if (directorio is null)
            return BadRequest("El directorio destino no existe.");

        // Se toma únicamente el nombre del archivo y se descarta cualquier ruta que mande el
        // cliente, para que el nombre recibido no pueda influir en la ubicación física.
        var nombreOriginal = Path.GetFileName(archivo.FileName);
        var extension = Path.GetExtension(nombreOriginal);
        var nombre = Path.GetFileNameWithoutExtension(nombreOriginal);

        if (string.IsNullOrWhiteSpace(nombre))
            nombre = "archivo";

        // Validación de entrada del cliente: evita desbordar los límites de las columnas.
        if (nombre.Length > LongitudMaximaNombre || extension.Length > LongitudMaximaExtension)
            return BadRequest("El nombre del archivo es demasiado largo.");

        // El contenido se guarda en el almacenamiento con un nombre físico generado
        // (GUID + extensión), de modo que dos archivos con el mismo nombre nunca choquen.
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
        await _contexto.SaveChangesAsync(cancelacion);

        return CreatedAtAction(nameof(ObtenerArchivo), new { id = registro.Id }, new ArchivoListado
        {
            Id = registro.Id,
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

    /// <summary>
    /// Consulta paginada de archivos de un directorio (requerimiento 3.5).
    /// </summary>
    /// <param name="directorioId">Directorio que se está consultando. Es obligatorio: la consulta se hace por directorio.</param>
    /// <param name="nombre">Subcadena del nombre del archivo</param>
    /// <param name="extension">Subcadena de la extensión, es decir, el tipo de archivo</param>
    /// <param name="propietarioId">Identificador del usuario que subió el archivo</param>
    /// <param name="subidoDesde">Fecha inicial de subida (inclusive)</param>
    /// <param name="subidoHasta">Fecha final de subida (inclusive)</param>
    /// <param name="pagina">Número de página (inicia en 1)</param>
    /// <param name="tamanoPagina">Tamaño de página (máximo 100)</param>
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

        if (!await _contexto.Directorios.AnyAsync(d => d.Id == directorioId))
            return NotFound("El directorio indicado no existe.");

        var consulta = _contexto.Archivos
            .AsNoTracking()
            .Where(a => a.DirectorioId == directorioId);

        // Los filtros de texto son por subcadena: "jo" encuentra "reporte_jo.pdf".
        if (!string.IsNullOrWhiteSpace(nombre))
            consulta = consulta.Where(a => a.Nombre.Contains(nombre));

        if (!string.IsNullOrWhiteSpace(extension))
            consulta = consulta.Where(a => a.Extension.Contains(extension));

        if (propietarioId.HasValue)
            consulta = consulta.Where(a => a.PropietarioId == propietarioId.Value);

        // El rango de fechas se convierte a instantes completos antes de enviarlo a la base de
        // datos: así se evita traducir DateTime.Date, que no todos los proveedores soportan.
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

    /// <summary>
    /// Consulta un archivo por su identificador.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ArchivoListado>> ObtenerArchivo(long id)
    {
        var archivo = await _contexto.Archivos
            .AsNoTracking()
            .Where(a => a.Id == id)
            .Select(ProyeccionListado)
            .FirstOrDefaultAsync();

        return archivo is null ? NotFound() : Ok(archivo);
    }

    /// <summary>
    /// Descarga el contenido de un archivo (requerimiento 3.6). Cualquier usuario autenticado
    /// puede descargar cualquier archivo del repositorio.
    /// </summary>
    [HttpGet("{id}/descargar")]
    public async Task<IActionResult> DescargarArchivo(long id)
    {
        var archivo = await _contexto.Archivos.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);

        if (archivo is null)
            return NotFound();

        var rutaCompleta = _almacenamiento.ObtenerRutaCompleta(archivo.NombreAlmacenamiento);

        // Si el registro existe pero el contenido físico no, se responde 404 en lugar de
        // dejar que la petición falle con un error interno.
        if (!System.IO.File.Exists(rutaCompleta))
            return NotFound("El contenido del archivo no está disponible en el almacenamiento.");

        // PhysicalFile entrega el archivo directamente desde el disco, sin cargarlo en memoria,
        // y el tercer parámetro indica el nombre con el que se guardará en el cliente.
        return PhysicalFile(rutaCompleta, archivo.TipoContenido, archivo.Nombre + archivo.Extension);
    }

    /// <summary>
    /// Renombra un archivo (requerimiento 3.4). Solo el usuario que lo subió o un administrador.
    /// </summary>
    [HttpPatch("{id}")]
    public async Task<IActionResult> RenombrarArchivo(long id, ModeloRenombrar modelo)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var archivo = await _contexto.Archivos.FirstOrDefaultAsync(a => a.Id == id);

        if (archivo is null)
            return NotFound();

        // Solo el usuario que subió el archivo o un administrador pueden editarlo.
        if (archivo.PropietarioId != UsuarioActualId() && !EsAdmin())
            return Forbid();

        // Se escribe únicamente la columna del nombre. La extensión vive en su propia columna
        // y no se toca, por lo que renombrar nunca puede cambiarla.
        archivo.Nombre = modelo.Nombre.Trim();
        archivo.ActualizadoEn = DateTime.UtcNow;

        await _contexto.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Elimina un archivo (requerimiento 3.3). Solo el usuario que lo subió o un administrador.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> EliminarArchivo(long id)
    {
        var archivo = await _contexto.Archivos.FirstOrDefaultAsync(a => a.Id == id);

        if (archivo is null)
            return NotFound();

        // Solo el usuario que subió el archivo o un administrador pueden eliminarlo.
        if (archivo.PropietarioId != UsuarioActualId() && !EsAdmin())
            return Forbid();

        _contexto.Archivos.Remove(archivo);
        await _contexto.SaveChangesAsync();

        // El contenido físico se borra después de guardar los cambios: si la operación en la
        // base de datos falla, el archivo sigue en disco en lugar de perderse sin registro.
        _almacenamiento.Eliminar(archivo.NombreAlmacenamiento);

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

    /// <summary>Indica si quien hace la petición es administrador (claim de rol = "True").</summary>
    private bool EsAdmin() => User.FindFirstValue(ClaimTypes.Role) == "True";

    /// <summary>
    /// Proyección reutilizada por las consultas. NombreCompleto se arma en la base de datos para
    /// que el cliente reciba el nombre tal como lo ve el usuario.
    /// </summary>
    private static readonly Expression<Func<Archivo, ArchivoListado>> ProyeccionListado = a => new ArchivoListado
    {
        Id = a.Id,
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
