namespace GestionInmobiliaria.Dominio.Common;

public class PaginationParams
{
    public static int TamanoPorDefecto { get; set; } = 10;
    public static int TamanoMaximo { get; set; } = 50;

    private int _tamano = TamanoPorDefecto;

    public int Pagina { get; set; } = 1;

    public int Tamano
    {
        get => _tamano;
        set => _tamano = value > TamanoMaximo ? TamanoMaximo : value < 1 ? 1 : value;
    }
}
