using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Proyecto.Models;

namespace Proyecto.Services;

/// <summary>Autorización central. Compartir concede lectura; nunca escritura ni acceso a los padres.</summary>
public class AccesoEquipos(ContextoProyecto contexto, IHttpContextAccessor http)
{
    public Usuario Actual => (Usuario)http.HttpContext!.Items["UsuarioActual"]!;
    public bool EsGlobal => Actual.EsAdmin && Actual.NivelAdmin == 2;
    public bool EsAdmin => Actual.EsAdmin;
    public bool PuedeEquipo(long equipoId) => EsGlobal || equipoId == Actual.EquipoId;
    public IQueryable<Usuario> Usuarios => EsGlobal ? contexto.Usuarios
        : contexto.Usuarios.Where(u => u.EquipoId == Actual.EquipoId);
    public IQueryable<Directorio> DirectoriosEscritura => EsGlobal ? contexto.Directorios
        : contexto.Directorios.Where(d => d.EquipoId == Actual.EquipoId);
    public IQueryable<Archivo> ArchivosEscritura => EsGlobal ? contexto.Archivos
        : contexto.Archivos.Where(a => a.Directorio.EquipoId == Actual.EquipoId);

    // Se calcula una vez por petición. La expansión solo sigue descendientes de carpetas compartidas.
    private List<long>? _directoriosCompartidos;
    private async Task<List<long>> DirectoriosCompartidosAsync()
    {
        if (_directoriosCompartidos is not null) return _directoriosCompartidos;
        var ids = (await contexto.ContenidosCompartidos
            .Where(c => c.EquipoDestinoId == Actual.EquipoId && c.DirectorioId != null)
            .Select(c => c.DirectorioId!.Value).ToListAsync()).ToHashSet();
        var nivel = ids.ToList();
        while (nivel.Count > 0)
        {
            var hijos = await contexto.Directorios.Where(d => d.PadreId != null && nivel.Contains(d.PadreId.Value))
                .Select(d => d.Id).ToListAsync();
            nivel = hijos.Where(ids.Add).ToList();
        }
        return _directoriosCompartidos = ids.ToList();
    }

    public async Task<IQueryable<Directorio>> DirectoriosLecturaAsync()
    {
        if (EsGlobal) return contexto.Directorios;
        var ids = await DirectoriosCompartidosAsync();
        return contexto.Directorios.Where(d => d.EquipoId == Actual.EquipoId || ids.Contains(d.Id));
    }

    public async Task<IQueryable<Archivo>> ArchivosLecturaAsync()
    {
        if (EsGlobal) return contexto.Archivos;
        var ids = await DirectoriosCompartidosAsync();
        return contexto.Archivos.Where(a => a.Directorio.EquipoId == Actual.EquipoId
            || ids.Contains(a.DirectorioId)
            || contexto.ContenidosCompartidos.Any(c => c.EquipoDestinoId == Actual.EquipoId && c.ArchivoId == a.Id));
    }
}
