namespace GestionInmobiliaria.Dominio.Common;

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Pagina { get; set; }
    public int Tamano { get; set; }
    public int TotalRegistros { get; set; }
    public int TotalPaginas { get; set; }
    public bool TienePaginaAnterior => Pagina > 1;
    public bool TienePaginaSiguiente => Pagina < TotalPaginas;
}
