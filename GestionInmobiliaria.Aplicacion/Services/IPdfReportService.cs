using GestionInmobiliaria.Aplicacion.DTOs;
using GestionInmobiliaria.Dominio.Common;

namespace GestionInmobiliaria.Aplicacion.Services;

public interface IPdfReportService
{
    byte[] GenerarPropietarios(IEnumerable<PropietarioDto> datos, PdfReportConfig config);
    byte[] GenerarPropiedades(IEnumerable<PropiedadDto> datos, PdfReportConfig config);
}
