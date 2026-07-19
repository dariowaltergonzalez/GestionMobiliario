using GestionInmobiliaria.Aplicacion.DTOs;
using GestionInmobiliaria.Dominio.Common;

namespace GestionInmobiliaria.Aplicacion.Services;

public interface IPdfReportService
{
    byte[] GenerarPropietarios(IEnumerable<PropietarioDto> datos, PdfReportConfig config);
    byte[] GenerarPropiedades(IEnumerable<PropiedadDto> datos, PdfReportConfig config);
    byte[] GenerarAgenda(IEnumerable<EventoAgendaDto> datos, PdfReportConfig config);
    byte[] GenerarTasaciones(IEnumerable<SolicitudTasacionDto> datos, PdfReportConfig config);
    byte[] GenerarReservas(IEnumerable<ReservaDto> datos, PdfReportConfig config);
    byte[] GenerarContrato(ContratoDto contrato, PdfReportConfig config, IEnumerable<ClausulaContratoDto> clausulas);
    byte[] GenerarReciboPago(PagoDto pago, ContratoDto contrato, PdfReportConfig config);
}
