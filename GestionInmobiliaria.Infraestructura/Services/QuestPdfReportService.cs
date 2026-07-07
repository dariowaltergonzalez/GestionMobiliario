using GestionInmobiliaria.Aplicacion.DTOs;
using GestionInmobiliaria.Aplicacion.Services;
using GestionInmobiliaria.Dominio.Common;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkiaSharp;
using Svg.Skia;

namespace GestionInmobiliaria.Infraestructura.Services;

public class QuestPdfReportService : IPdfReportService
{
    private static readonly string AzulOscuro = "#1e3a5f";
    private static readonly string GrisClaro = "#f8f9fa";

    private readonly ILogger<QuestPdfReportService> _logger;

    static QuestPdfReportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public QuestPdfReportService(ILogger<QuestPdfReportService> logger)
    {
        _logger = logger;
    }

    public byte[] GenerarPropietarios(IEnumerable<PropietarioDto> datos, PdfReportConfig config)
    {
        var lista = datos.ToList();

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.MarginTop(0.6f, Unit.Centimetre);
                page.MarginBottom(1f, Unit.Centimetre);
                page.MarginHorizontal(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(8));

                page.Header().Element(h => ComposeHeader(h, config, lista.Count));
                page.Content().Element(c => ComposePropietariosTable(c, lista));
                page.Footer().Element(ComposeFooter);
            });
        }).GeneratePdf();
    }

    public byte[] GenerarAgenda(IEnumerable<EventoAgendaDto> datos, PdfReportConfig config)
    {
        var lista = datos.ToList();

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.MarginTop(0.6f, Unit.Centimetre);
                page.MarginBottom(1f, Unit.Centimetre);
                page.MarginHorizontal(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(8));

                page.Header().Element(h => ComposeHeader(h, config, lista.Count));
                page.Content().Element(c => ComposeAgendaTable(c, lista));
                page.Footer().Element(ComposeFooter);
            });
        }).GeneratePdf();
    }

    public byte[] GenerarReservas(IEnumerable<ReservaDto> datos, PdfReportConfig config)
    {
        var lista = datos.ToList();

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.MarginTop(0.6f, Unit.Centimetre);
                page.MarginBottom(1f, Unit.Centimetre);
                page.MarginHorizontal(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(8));

                page.Header().Element(h => ComposeHeader(h, config, lista.Count));
                page.Content().Element(c => ComposeReservasTable(c, lista));
                page.Footer().Element(ComposeFooter);
            });
        }).GeneratePdf();
    }

    public byte[] GenerarTasaciones(IEnumerable<SolicitudTasacionDto> datos, PdfReportConfig config)
    {
        var lista = datos.ToList();

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.MarginTop(0.6f, Unit.Centimetre);
                page.MarginBottom(1f, Unit.Centimetre);
                page.MarginHorizontal(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(8));

                page.Header().Element(h => ComposeHeader(h, config, lista.Count));
                page.Content().Element(c => ComposeTasacionesTable(c, lista));
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
                page.MarginTop(0.6f, Unit.Centimetre);
                page.MarginBottom(1f, Unit.Centimetre);
                page.MarginHorizontal(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(8));

                page.Header().Element(h => ComposeHeader(h, config, lista.Count));
                page.Content().Element(c => ComposePropiedadesTable(c, lista));
                page.Footer().Element(ComposeFooter);
            });
        }).GeneratePdf();
    }

    private void ComposeHeader(IContainer container, PdfReportConfig config, int totalRegistros)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                var logoBytes = NormalizarImagen(config.LogoBytes);
                if (logoBytes is { Length: > 0 })
                {
                    _logger.LogInformation("PDF: renderizando logo ({Bytes} bytes)", logoBytes.Length);
                    row.ConstantItem(75).AlignMiddle().PaddingRight(12)
                        .MaxHeight(55).Image(logoBytes).FitArea();
                }
                else
                {
                    _logger.LogWarning("PDF: logo omitido (logoBytes={LogoBytesNull})", logoBytes == null ? "null" : "0 bytes");
                }

                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(config.NombreEmpresa)
                        .FontSize(14).Bold().FontColor(AzulOscuro);

                    if (!string.IsNullOrWhiteSpace(config.Slogan))
                        c.Item().Text(config.Slogan)
                            .FontSize(8).Italic().FontColor(Colors.Grey.Medium);

                    if (!string.IsNullOrWhiteSpace(config.InfoEmpresa))
                        c.Item().PaddingTop(2).Text(config.InfoEmpresa)
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

            col.Item().PaddingTop(2).LineHorizontal(1.5f).LineColor(AzulOscuro);
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
                cols.RelativeColumn(3);
                cols.RelativeColumn(2);
                cols.RelativeColumn(2.5f);
                cols.RelativeColumn(3);
                cols.RelativeColumn(2);
                cols.RelativeColumn(2);
                cols.ConstantColumn(40);
                cols.ConstantColumn(55);
            });

            table.Header(header =>
            {
                string[] cols = ["Apellido y Nombre", "DNI", "CUIT", "Email", "Teléfono", "Dirección", "Props.", "Alta"];
                foreach (var col in cols)
                    header.Cell().Background(AzulOscuro).Padding(5)
                        .Text(col).FontColor("#FFFFFF").Bold().FontSize(7.5f);
            });

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
                cols.ConstantColumn(70);
                cols.RelativeColumn(4);
                cols.RelativeColumn(2);
                cols.RelativeColumn(2);
                cols.RelativeColumn(2);
                cols.RelativeColumn(2);
                cols.RelativeColumn(2);
                cols.RelativeColumn(3);
            });

            table.Header(header =>
            {
                string[] cols = ["Código", "Dirección", "Tipo", "Operación", "Estado", "Alquiler (ARS)", "Venta (USD)", "Propietario"];
                foreach (var col in cols)
                    header.Cell().Background(AzulOscuro).Padding(5)
                        .Text(col).FontColor("#FFFFFF").Bold().FontSize(7.5f);
            });

            for (int i = 0; i < lista.Count; i++)
            {
                var p = lista[i];
                var bg = i % 2 == 0 ? "#FFFFFF" : GrisClaro;

                Cell(table, bg, p.Codigo, bold: true);
                Cell(table, bg, p.DireccionCompleta);
                Cell(table, bg, MapTipo(p.Tipo.ToString()));
                Cell(table, bg, MapOperacion(p.Operacion.ToString()));
                Cell(table, bg, MapEstado(p.Estado.ToString()));
                Cell(table, bg, p.PrecioAlquiler.HasValue ? $"$ {p.PrecioAlquiler.Value:N0}" : "—");
                Cell(table, bg, p.PrecioVenta.HasValue ? $"U$S {p.PrecioVenta.Value:N0}" : "—");
                Cell(table, bg, p.PropietarioNombre);
            }
        });
    }

    private static void ComposeAgendaTable(IContainer container, List<EventoAgendaDto> lista)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(2);
                cols.RelativeColumn(3);
                cols.RelativeColumn(3);
                cols.RelativeColumn(3);
                cols.RelativeColumn(2.5f);
                cols.RelativeColumn(2);
                cols.RelativeColumn(4);
            });

            table.Header(header =>
            {
                string[] cols = ["Tipo", "Fecha y hora", "Contacto", "Propiedad", "Agente", "Estado", "Notas"];
                foreach (var col in cols)
                    header.Cell().Background(AzulOscuro).Padding(5)
                        .Text(col).FontColor("#FFFFFF").Bold().FontSize(7.5f);
            });

            for (int i = 0; i < lista.Count; i++)
            {
                var e = lista[i];
                var bg = i % 2 == 0 ? "#FFFFFF" : GrisClaro;
                var fechaLocal = e.FechaHora.ToLocalTime();

                Cell(table, bg, MapTipoEvento(e.Tipo), bold: true);
                Cell(table, bg, fechaLocal.ToString("dd/MM/yyyy HH:mm"));
                Cell(table, bg, e.LeadNombre ?? e.InquilinoNombre ?? "—");
                Cell(table, bg, e.PropiedadDireccion ?? "—");
                Cell(table, bg, e.AgenteNombre);
                Cell(table, bg, MapEstadoEvento(e.Estado));
                Cell(table, bg, e.Notas ?? "—");
            }
        });
    }

    private static void ComposeReservasTable(IContainer container, List<ReservaDto> lista)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(3);
                cols.RelativeColumn(3);
                cols.RelativeColumn(3.5f);
                cols.RelativeColumn(2);
                cols.RelativeColumn(2.5f);
                cols.RelativeColumn(2.5f);
                cols.RelativeColumn(2);
                cols.RelativeColumn(2);
            });

            table.Header(header =>
            {
                string[] cols = ["Comprador", "Vendedor", "Propiedad", "Seña", "Precio total", "Estado", "Fec. reserva", "Vencimiento"];
                foreach (var col in cols)
                    header.Cell().Background(AzulOscuro).Padding(5)
                        .Text(col).FontColor("#FFFFFF").Bold().FontSize(7.5f);
            });

            for (int i = 0; i < lista.Count; i++)
            {
                var r = lista[i];
                var bg = i % 2 == 0 ? "#FFFFFF" : GrisClaro;
                var simbolo = r.Moneda == "USD" ? "U$S" : "$";

                Cell(table, bg, $"{r.CompradorApellido}, {r.CompradorNombre}", bold: true);
                Cell(table, bg, $"{r.VendedorApellido}, {r.VendedorNombre}");
                Cell(table, bg, r.PropiedadDireccion);
                Cell(table, bg, $"{simbolo} {r.MontoSenia:N0}");
                Cell(table, bg, r.PrecioTotal.HasValue ? $"{simbolo} {r.PrecioTotal.Value:N0}" : "—");
                Cell(table, bg, MapEstadoReserva(r.Estado));
                Cell(table, bg, r.FechaReserva.ToString("dd/MM/yyyy"));
                Cell(table, bg, r.FechaVencimiento.ToString("dd/MM/yyyy"));
            }
        });
    }

    private static string MapEstadoReserva(string estado) => estado switch
    {
        "Vigente"    => "Vigente",
        "Vencida"    => "Vencida",
        "Cancelada"  => "Cancelada",
        "Convertida" => "Convertida",
        _            => estado
    };

    private static void ComposeTasacionesTable(IContainer container, List<SolicitudTasacionDto> lista)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(3);
                cols.RelativeColumn(2.5f);
                cols.RelativeColumn(2);
                cols.RelativeColumn(3.5f);
                cols.RelativeColumn(2);
                cols.RelativeColumn(2.5f);
                cols.RelativeColumn(2.5f);
                cols.ConstantColumn(60);
            });

            table.Header(header =>
            {
                string[] cols = ["Solicitante", "Teléfono / Email", "Tipo", "Dirección", "Estado", "Agente", "Valor estimado", "Fec. solicitud"];
                foreach (var col in cols)
                    header.Cell().Background(AzulOscuro).Padding(5)
                        .Text(col).FontColor("#FFFFFF").Bold().FontSize(7.5f);
            });

            for (int i = 0; i < lista.Count; i++)
            {
                var t = lista[i];
                var bg = i % 2 == 0 ? "#FFFFFF" : GrisClaro;

                Cell(table, bg, $"{t.Apellido}, {t.Nombre}", bold: true);
                Cell(table, bg, string.Join("\n", new[] { t.Telefono, t.Email }.Where(x => !string.IsNullOrWhiteSpace(x))!));
                Cell(table, bg, MapTipoPropiedad(t.TipoPropiedad));
                Cell(table, bg, string.IsNullOrWhiteSpace(t.Barrio) ? t.Direccion : $"{t.Direccion}, {t.Barrio}");
                Cell(table, bg, MapEstadoTasacion(t.Estado));
                Cell(table, bg, t.NombreAgente ?? "Sin asignar");
                Cell(table, bg, t.ValorEstimado.HasValue ? $"$ {t.ValorEstimado.Value:N0}" : "—");
                Cell(table, bg, t.FechaCreacion.ToString("dd/MM/yyyy"));
            }
        });
    }

    private static string MapTipoPropiedad(string tipo) => tipo switch
    {
        "Departamento" => "Depto.",
        "Galpon"       => "Galpón",
        _              => tipo
    };

    private static string MapEstadoTasacion(string estado) => estado switch
    {
        "Pendiente"   => "Pendiente",
        "Asignada"    => "Asignada",
        "EnProceso"   => "En proceso",
        "Completada"  => "Completada",
        "Cancelada"   => "Cancelada",
        _             => estado
    };

    private static string MapTipoEvento(string tipo) => tipo switch
    {
        "Visita"  => "Visita",
        "Llamada" => "Llamada",
        "Reunion" => "Reunión",
        _         => "Otro"
    };

    private static string MapEstadoEvento(string estado) => estado switch
    {
        "Pendiente"  => "Pendiente",
        "Realizada"  => "Realizada",
        "Cancelada"  => "Cancelada",
        _            => estado
    };

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

    // Convierte cualquier formato de imagen (PNG, JPEG, BMP, GIF, SVG) a JPEG para QuestPDF
    private byte[]? NormalizarImagen(byte[]? bytes)
    {
        if (bytes is null or { Length: 0 }) return null;

        // Detectar SVG por su cabecera de texto
        if (EsSvg(bytes))
            return ConvertirSvgAJpeg(bytes);

        // Raster (PNG, JPEG, BMP, GIF, etc.) → JPEG via System.Drawing
        try
        {
            using var ms = new MemoryStream(bytes);
            using var bmp = new System.Drawing.Bitmap(ms);
            using var outMs = new MemoryStream();
            bmp.Save(outMs, System.Drawing.Imaging.ImageFormat.Jpeg);
            var result = outMs.ToArray();
            return result.Length > 0 ? result : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PDF-Logo: no se pudo convertir la imagen raster");
            return null;
        }
    }

    private static bool EsSvg(byte[] bytes)
    {
        // SVG empieza con '<svg' o '<?xml'
        var inicio = System.Text.Encoding.UTF8.GetString(bytes[..Math.Min(20, bytes.Length)]).TrimStart();
        return inicio.StartsWith("<svg", StringComparison.OrdinalIgnoreCase)
            || inicio.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase);
    }

    private byte[]? ConvertirSvgAJpeg(byte[] bytes)
    {
        try
        {
            using var svg = new SKSvg();
            using var ms = new MemoryStream(bytes);
            var picture = svg.Load(ms);
            if (picture is null) return null;

            var bounds = picture.CullRect;
            int w = Math.Max((int)bounds.Width, 1);
            int h = Math.Max((int)bounds.Height, 1);

            using var bitmap = new SKBitmap(w, h);
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColors.White);
            canvas.DrawPicture(picture);
            canvas.Flush();

            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, 90);
            return data?.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PDF-Logo: no se pudo convertir el SVG");
            return null;
        }
    }
}
