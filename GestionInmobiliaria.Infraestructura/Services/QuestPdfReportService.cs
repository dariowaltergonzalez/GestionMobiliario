using GestionInmobiliaria.Aplicacion.DTOs;
using GestionInmobiliaria.Aplicacion.Services;
using GestionInmobiliaria.Dominio.Common;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GestionInmobiliaria.Infraestructura.Services;

public class QuestPdfReportService : IPdfReportService
{
    private static readonly string AzulOscuro = "#1e3a5f";
    private static readonly string GrisClaro = "#f8f9fa";

    static QuestPdfReportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerarPropietarios(IEnumerable<PropietarioDto> datos, PdfReportConfig config)
    {
        var lista = datos.ToList();

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(8));

                page.Header().Element(h => ComposeHeader(h, config, lista.Count));
                page.Content().PaddingTop(10).Element(c => ComposePropietariosTable(c, lista));
                page.Footer().Element(ComposeFooter);
            });
        }).GeneratePdf();
    }

    public byte[] GenerarPropiedades(IEnumerable<PropiedadDto> datos, PdfReportConfig config)
    {
        var lista = datos.ToList();

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(8));

                page.Header().Element(h => ComposeHeader(h, config, lista.Count));
                page.Content().PaddingTop(10).Element(c => ComposePropiedadesTable(c, lista));
                page.Footer().Element(ComposeFooter);
            });
        }).GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, PdfReportConfig config, int totalRegistros)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(config.NombreEmpresa)
                        .FontSize(14).Bold().FontColor(AzulOscuro);

                    if (!string.IsNullOrWhiteSpace(config.InfoEmpresa))
                        c.Item().Text(config.InfoEmpresa)
                            .FontSize(7).FontColor(Colors.Grey.Darken2);
                });

                row.ConstantItem(180).AlignRight().Column(c =>
                {
                    c.Item().Text(config.Titulo)
                        .FontSize(11).Bold().FontColor(AzulOscuro);
                    c.Item().Text($"Generado: {config.FechaGeneracion:dd/MM/yyyy HH:mm}")
                        .FontSize(7).FontColor(Colors.Grey.Darken2);
                    c.Item().Text($"Total: {totalRegistros} registro{(totalRegistros != 1 ? "s" : "")}")
                        .FontSize(7).FontColor(Colors.Grey.Darken2);
                });
            });

            if (!string.IsNullOrWhiteSpace(config.FiltrosAplicados))
            {
                col.Item().PaddingTop(4).Text($"Filtros: {config.FiltrosAplicados}")
                    .FontSize(7).Italic().FontColor(Colors.Grey.Darken1);
            }

            col.Item().PaddingTop(6).LineHorizontal(1.5f).LineColor(AzulOscuro);
        });
    }

    private static void ComposeFooter(IContainer container)
    {
        container.PaddingTop(6).Row(row =>
        {
            row.RelativeItem().Text("GestionInmobiliaria")
                .FontSize(7).FontColor(Colors.Grey.Medium);
            row.RelativeItem().AlignRight().Text(x =>
            {
                x.Span("Página ").FontSize(7).FontColor(Colors.Grey.Medium);
                x.CurrentPageNumber().FontSize(7).FontColor(Colors.Grey.Medium);
                x.Span(" de ").FontSize(7).FontColor(Colors.Grey.Medium);
                x.TotalPages().FontSize(7).FontColor(Colors.Grey.Medium);
            });
        });
    }

    private static void ComposePropietariosTable(IContainer container, List<PropietarioDto> lista)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(3);  // Apellido y nombre
                cols.RelativeColumn(2);  // DNI
                cols.RelativeColumn(2.5f); // CUIT
                cols.RelativeColumn(3);  // Email
                cols.RelativeColumn(2);  // Teléfono
                cols.RelativeColumn(2);  // Dirección
                cols.ConstantColumn(40); // Propiedades
                cols.ConstantColumn(55); // Alta
            });

            // Encabezado
            table.Header(header =>
            {
                string[] cols = ["Apellido y Nombre", "DNI", "CUIT", "Email", "Teléfono", "Dirección", "Props.", "Alta"];
                foreach (var col in cols)
                    header.Cell().Background(AzulOscuro).Padding(5)
                        .Text(col).FontColor("#FFFFFF").Bold().FontSize(7.5f);
            });

            // Filas
            for (int i = 0; i < lista.Count; i++)
            {
                var p = lista[i];
                var bg = i % 2 == 0 ? "#FFFFFF" : GrisClaro;

                Cell(table, bg, $"{p.Apellido}, {p.Nombre}", bold: true);
                Cell(table, bg, p.Dni ?? "—");
                Cell(table, bg, p.Cuit ?? "—");
                Cell(table, bg, p.Email ?? "—");
                Cell(table, bg, p.Telefono ?? "—");
                Cell(table, bg, p.Direccion ?? "—");
                table.Cell().Background(bg).Padding(5).AlignCenter()
                    .Text(p.CantidadPropiedades.ToString()).FontSize(8);
                Cell(table, bg, p.FechaCreacion.ToString("dd/MM/yyyy"));
            }
        });
    }

    private static void ComposePropiedadesTable(IContainer container, List<PropiedadDto> lista)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(4);  // Dirección
                cols.RelativeColumn(2);  // Tipo
                cols.RelativeColumn(2);  // Operación
                cols.RelativeColumn(2);  // Estado
                cols.RelativeColumn(2);  // Precio Alquiler
                cols.RelativeColumn(2);  // Precio Venta
                cols.RelativeColumn(3);  // Propietario
            });

            // Encabezado
            table.Header(header =>
            {
                string[] cols = ["Dirección", "Tipo", "Operación", "Estado", "Alquiler (ARS)", "Venta (USD)", "Propietario"];
                foreach (var col in cols)
                    header.Cell().Background(AzulOscuro).Padding(5)
                        .Text(col).FontColor("#FFFFFF").Bold().FontSize(7.5f);
            });

            // Filas
            for (int i = 0; i < lista.Count; i++)
            {
                var p = lista[i];
                var bg = i % 2 == 0 ? "#FFFFFF" : GrisClaro;

                Cell(table, bg, p.DireccionCompleta, bold: true);
                Cell(table, bg, MapTipo(p.Tipo.ToString()));
                Cell(table, bg, MapOperacion(p.Operacion.ToString()));
                Cell(table, bg, MapEstado(p.Estado.ToString()));
                Cell(table, bg, p.PrecioAlquiler.HasValue ? $"$ {p.PrecioAlquiler.Value:N0}" : "—");
                Cell(table, bg, p.PrecioVenta.HasValue ? $"U$S {p.PrecioVenta.Value:N0}" : "—");
                Cell(table, bg, p.PropietarioNombre);
            }
        });
    }

    private static void Cell(TableDescriptor table, string bg, string text, bool bold = false)
    {
        var cell = table.Cell().Background(bg).Padding(5);
        if (bold)
            cell.Text(x => x.Span(text).Bold().FontSize(8));
        else
            cell.Text(text).FontSize(8);
    }

    private static string MapTipo(string tipo) => tipo switch
    {
        "Departamento" => "Depto.",
        "Terreno" => "Terreno",
        "Galpon" => "Galpón",
        "Oficina" => "Oficina",
        "Local" => "Local",
        "Casa" => "Casa",
        "PH" => "PH",
        _ => tipo
    };

    private static string MapOperacion(string op) => op switch
    {
        "Alquiler" => "Alquiler",
        "Venta" => "Venta",
        "AlquilerOVenta" => "Alq. o Venta",
        _ => op
    };

    private static string MapEstado(string estado) => estado switch
    {
        "Disponible" => "Disponible",
        "Alquilada" => "Alquilada",
        "EnMantenimiento" => "En Mant.",
        "NoDisponible" => "No Disponible",
        "Vendida" => "Vendida",
        "Reservada" => "Reservada",
        _ => estado
    };
}
