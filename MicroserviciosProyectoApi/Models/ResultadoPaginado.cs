namespace Proyecto.Models;

public class ResultadoPaginado<T>
{

    public IEnumerable<T> Elementos { get; set; } = [];

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }

    public int TotalRegistros { get; set; }

    public int TotalPaginas => TamanoPagina < 1 ? 0 : (int)Math.Ceiling(TotalRegistros / (double)TamanoPagina);
}
