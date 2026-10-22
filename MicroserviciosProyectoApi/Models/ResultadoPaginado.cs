namespace Proyecto.Models;

/// <summary>
/// Envoltura genérica de una respuesta paginada. Evita repetir la misma forma de
/// respuesta en cada consulta de lista (usuarios, directorios y archivos).
/// </summary>
public class ResultadoPaginado<T>
{
    /// <summary>Registros de la página solicitada.</summary>
    public IEnumerable<T> Elementos { get; set; } = [];

    /// <summary>Número de página devuelto (inicia en 1).</summary>
    public int Pagina { get; set; }

    /// <summary>Cantidad máxima de registros por página.</summary>
    public int TamanoPagina { get; set; }

    /// <summary>Total de registros que cumplen el filtro, sin paginar.</summary>
    public int TotalRegistros { get; set; }

    /// <summary>Total de páginas, calculado a partir de TotalRegistros y TamanoPagina.</summary>
    public int TotalPaginas => TamanoPagina < 1 ? 0 : (int)Math.Ceiling(TotalRegistros / (double)TamanoPagina);
}
