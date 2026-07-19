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

    // ─── Contrato de Locación ──────────────────────────────────────────────────

    public byte[] GenerarContrato(ContratoDto c, PdfReportConfig config, IEnumerable<ClausulaContratoDto> clausulas)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginTop(1.5f, Unit.Centimetre);
                page.MarginBottom(2f, Unit.Centimetre);
                page.MarginHorizontal(2.2f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9f).FontFamily("Arial"));

                page.Header().Element(h => ComposeContratoHeader(h, c, config));
                page.Content().Element(cnt => ComposeContratoClauses(cnt, c, config, clausulas));
                page.Footer().Element(ComposeFooter);
            });
        }).GeneratePdf();
    }

    private static void ComposeContratoHeader(IContainer container, ContratoDto c, PdfReportConfig config)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(info =>
                {
                    info.Item().Text(config.NombreEmpresa).FontSize(13).Bold().FontColor(AzulOscuro);
                    if (!string.IsNullOrWhiteSpace(config.InfoEmpresa))
                        info.Item().Text(config.InfoEmpresa).FontSize(7).FontColor(Colors.Grey.Darken2);
                });
                row.ConstantItem(160).AlignRight().Column(cod =>
                {
                    cod.Item().Text("CONTRATO DE LOCACIÓN").FontSize(10).Bold().FontColor(AzulOscuro);
                    cod.Item().Text(c.Codigo).FontSize(9).FontColor(AzulOscuro);
                    cod.Item().Text($"Generado: {DateTime.Now:dd/MM/yyyy}").FontSize(7).FontColor(Colors.Grey.Darken2);
                });
            });
            col.Item().PaddingTop(4).LineHorizontal(1.5f).LineColor(AzulOscuro);
            col.Item().PaddingTop(8);
        });
    }

    private static void ComposeContratoClauses(IContainer container, ContratoDto c, PdfReportConfig config, IEnumerable<ClausulaContratoDto> clausulas)
    {
        var vars = BuildPlaceholders(c, config);
        var ciudad = vars["{ciudad}"];

        container.Column(col =>
        {
            col.Spacing(0);

            col.Item().AlignCenter().Text("CONTRATO DE LOCACIÓN DE VIVIENDA")
                .FontSize(12).Bold().FontColor(AzulOscuro);
            col.Item().AlignCenter().Text("Ley N° 27551 — Código Civil y Comercial de la Nación")
                .FontSize(8).Italic().FontColor(Colors.Grey.Darken1);
            col.Item().PaddingTop(10);

            foreach (var cl in clausulas)
                Clausula(col, cl.Numero, cl.Titulo, Interpolar(cl.Texto, vars));

            if (!string.IsNullOrWhiteSpace(c.Observaciones))
            {
                col.Item().PaddingTop(6);
                col.Item().Text("OBSERVACIONES:").FontSize(8.5f).Bold().FontColor(AzulOscuro);
                col.Item().PaddingTop(2).Text(c.Observaciones).FontSize(8.5f);
            }

            col.Item().PaddingTop(20);
            col.Item().Text($"En {ciudad}, a los ____ días del mes de _________________________ de ________.")
                .FontSize(9f).Italic();

            col.Item().PaddingTop(24).Row(row =>
            {
                row.RelativeItem().Column(f =>
                {
                    f.Item().LineHorizontal(0.5f).LineColor(Colors.Black);
                    f.Item().PaddingTop(4).Text($"LOCADOR/A\n{c.LocadorNombre} {c.LocadorApellido}")
                        .FontSize(8.5f).AlignCenter();
                });
                row.ConstantItem(40);
                row.RelativeItem().Column(f =>
                {
                    f.Item().LineHorizontal(0.5f).LineColor(Colors.Black);
                    f.Item().PaddingTop(4).Text($"LOCATARIO/A\n{c.LocatarioNombre} {c.LocatarioApellido}")
                        .FontSize(8.5f).AlignCenter();
                });
            });

            if (!string.IsNullOrWhiteSpace(c.GaranteNombre))
            {
                col.Item().PaddingTop(24).Row(row =>
                {
                    row.RelativeItem(2).Column(f =>
                    {
                        f.Item().LineHorizontal(0.5f).LineColor(Colors.Black);
                        f.Item().PaddingTop(4).Text($"GARANTE/FIADOR\n{c.GaranteNombre} {c.GaranteApellido}")
                            .FontSize(8.5f).AlignCenter();
                    });
                    row.RelativeItem(3);
                });
            }
        });
    }

    private static Dictionary<string, string> BuildPlaceholders(ContratoDto c, PdfReportConfig config)
    {
        var moneda = c.Moneda == "USD" ? "U$S" : "$";
        var monto = $"{moneda} {c.MontoBase:N0}";
        var duracionMeses = c.FechaFin.HasValue
            ? ((c.FechaFin.Value.Year - c.FechaInicio.Year) * 12 + c.FechaFin.Value.Month - c.FechaInicio.Month).ToString()
            : "36";
        var ciudad = config.InfoEmpresa?.Split('|').LastOrDefault()?.Trim() ?? "Buenos Aires";

        var locador = $"{c.LocadorNombre} {c.LocadorApellido}" +
            (string.IsNullOrWhiteSpace(c.LocadorDni) ? "" : $" (DNI {c.LocadorDni})");
        var locatario = $"{c.LocatarioNombre} {c.LocatarioApellido}" +
            (string.IsNullOrWhiteSpace(c.LocatarioDni) ? "" : $" (DNI {c.LocatarioDni})");

        var ajusteTexto = c.TipoAjuste switch
        {
            "IndiceICL"  => "conforme el art. 14 de la Ley N° 27737 (Índice ICL)",
            "Porcentaje" => "por porcentaje pactado entre las partes",
            "Fijo"       => "sin ajuste (monto fijo durante toda la vigencia)",
            _            => "conforme lo acordado entre las partes",
        };

        string pagoMedio;
        if (!string.IsNullOrWhiteSpace(c.LocadorCbu))
        {
            pagoMedio = "El pago se efectuará por transferencia electrónica o depósito bancario";
            if (!string.IsNullOrWhiteSpace(c.LocadorBanco)) pagoMedio += $" en el Banco {c.LocadorBanco}";
            pagoMedio += $", CBU {c.LocadorCbu}";
            if (!string.IsNullOrWhiteSpace(c.LocadorCuit)) pagoMedio += $", CUIT {c.LocadorCuit}";
            pagoMedio += ", titular del/la LOCADOR/A. En contrapartida, EL/LA LOCADOR/A extenderá la factura electrónica correspondiente dentro de las 72hs (Res. N° 4004-E AFIP).";
        }
        else
        {
            pagoMedio = "El medio y lugar de pago serán acordados entre las partes.";
        }

        string garanteTexto;
        if (!string.IsNullOrWhiteSpace(c.GaranteNombre))
        {
            garanteTexto = $"Actúa como garante/fiador {c.GaranteNombre} {c.GaranteApellido}" +
                (string.IsNullOrWhiteSpace(c.GaranteDni) ? "" : $" (DNI {c.GaranteDni})") +
                (string.IsNullOrWhiteSpace(c.GaranteTelefono) ? "" : $", Tel: {c.GaranteTelefono}") +
                ", quien se obliga en forma solidaria al cumplimiento de todas las obligaciones emergentes del presente contrato.";
        }
        else
        {
            garanteTexto = "Las partes podrán convenir la garantía que corresponda conforme la Ley N° 27551.";
        }

        var locadorDomicilio = string.IsNullOrWhiteSpace(c.LocadorDomicilio) ? "___________________________" : c.LocadorDomicilio;
        var locadorEmail    = string.IsNullOrWhiteSpace(c.LocadorEmail)    ? "_______@_______.com" : c.LocadorEmail;
        var locatarioEmail  = string.IsNullOrWhiteSpace(c.LocatarioEmail)  ? "_______@_______.com" : c.LocatarioEmail;

        return new Dictionary<string, string>
        {
            // ── Compatibilidad con variables antiguas ────────────────────────
            { "{locador}",            locador },
            { "{locatario}",          locatario },
            { "{locadorDomicilio}",   locadorDomicilio },
            { "{propiedadDireccion}", c.PropiedadDireccion },
            { "{montoAlquiler}",      monto },
            { "{duracionMeses}",      duracionMeses },
            { "{fechaInicio}",        c.FechaInicio.ToString("dd/MM/yyyy") },
            { "{fechaFin}",           c.FechaFin.HasValue ? c.FechaFin.Value.ToString("dd/MM/yyyy") : "______/__/____" },
            { "{mesInicio}",          c.FechaInicio.ToString("MMMM yyyy", new System.Globalization.CultureInfo("es-AR")).ToUpper() },
            { "{ajusteTexto}",        ajusteTexto },
            { "{periodicidad}",       c.PeriodicidadAjusteMeses.HasValue ? $"cada {c.PeriodicidadAjusteMeses} meses" : "según lo convenido" },
            { "{diaVencimiento}",     c.DiaVencimientoPago.HasValue ? $", hasta el día {c.DiaVencimientoPago} de cada mes" : "" },
            { "{pagoMedio}",          pagoMedio },
            { "{garanteTexto}",       garanteTexto },
            { "{ciudad}",             ciudad },
            { "{locadorEmail}",       locadorEmail },
            { "{locatarioEmail}",     locatarioEmail },

            // ── Locador ──────────────────────────────────────────────────────
            { "{locador.nombreCompleto}", $"{c.LocadorNombre} {c.LocadorApellido}" },
            { "{locador.nombre}",         c.LocadorNombre },
            { "{locador.apellido}",       c.LocadorApellido },
            { "{locador.dni}",            c.LocadorDni ?? "" },
            { "{locador.email}",          locadorEmail },
            { "{locador.telefono}",       c.LocadorTelefono ?? "" },
            { "{locador.domicilio}",      locadorDomicilio },
            { "{locador.banco}",          c.LocadorBanco ?? "" },
            { "{locador.cbu}",            c.LocadorCbu ?? "" },
            { "{locador.cuit}",           c.LocadorCuit ?? "" },

            // ── Locatario ────────────────────────────────────────────────────
            { "{locatario.nombreCompleto}", $"{c.LocatarioNombre} {c.LocatarioApellido}" },
            { "{locatario.nombre}",         c.LocatarioNombre },
            { "{locatario.apellido}",       c.LocatarioApellido },
            { "{locatario.dni}",            c.LocatarioDni ?? "" },
            { "{locatario.email}",          locatarioEmail },
            { "{locatario.telefono}",       c.LocatarioTelefono ?? "" },

            // ── Propiedad ────────────────────────────────────────────────────
            { "{propiedad.direccion}", c.PropiedadDireccion },
            { "{propiedad.codigo}",    c.PropiedadCodigo ?? "" },

            // ── Garante ──────────────────────────────────────────────────────
            { "{garante.nombreCompleto}", $"{c.GaranteNombre ?? ""} {c.GaranteApellido ?? ""}".Trim() },
            { "{garante.nombre}",         c.GaranteNombre ?? "" },
            { "{garante.apellido}",       c.GaranteApellido ?? "" },
            { "{garante.dni}",            c.GaranteDni ?? "" },
            { "{garante.telefono}",       c.GaranteTelefono ?? "" },
            { "{garante.texto}",          garanteTexto },

            // ── Contrato ─────────────────────────────────────────────────────
            { "{contrato.montoAlquiler}",  monto },
            { "{contrato.duracionMeses}",  duracionMeses },
            { "{contrato.fechaInicio}",    c.FechaInicio.ToString("dd/MM/yyyy") },
            { "{contrato.fechaFin}",       c.FechaFin.HasValue ? c.FechaFin.Value.ToString("dd/MM/yyyy") : "______/__/____" },
            { "{contrato.mesInicio}",      c.FechaInicio.ToString("MMMM yyyy", new System.Globalization.CultureInfo("es-AR")).ToUpper() },
            { "{contrato.ajusteTexto}",    ajusteTexto },
            { "{contrato.periodicidad}",   c.PeriodicidadAjusteMeses.HasValue ? $"cada {c.PeriodicidadAjusteMeses} meses" : "según lo convenido" },
            { "{contrato.diaVencimiento}", c.DiaVencimientoPago.HasValue ? $", hasta el día {c.DiaVencimientoPago} de cada mes" : "" },
            { "{contrato.pagoMedio}",      pagoMedio },
            { "{contrato.garanteTexto}",   garanteTexto },

            // ── Empresa ──────────────────────────────────────────────────────
            { "{empresa.nombre}",   config.NombreEmpresa },
            { "{empresa.ciudad}",   ciudad },
        };
    }

    private static string Interpolar(string texto, Dictionary<string, string> vars)
    {
        foreach (var (key, value) in vars)
            texto = texto.Replace(key, value);
        return texto;
    }

    // ─── Recibo de Pago ───────────────────────────────────────────────────────

    public byte[] GenerarReciboPago(PagoDto pago, ContratoDto contrato, PdfReportConfig config)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.MarginTop(1.5f, Unit.Centimetre);
                page.MarginBottom(1.5f, Unit.Centimetre);
                page.MarginHorizontal(1.8f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9f).FontFamily("Arial"));

                page.Content().Element(cnt => ComposeRecibo(cnt, pago, contrato, config));
                page.Footer().Element(ComposeFooter);
            });
        }).GeneratePdf();
    }

    private static void ComposeRecibo(IContainer container, PagoDto pago, ContratoDto contrato, PdfReportConfig config)
    {
        var moneda = contrato.Moneda == "USD" ? "U$S" : "$";
        var monto = $"{moneda} {(pago.MontoPagado ?? pago.MontoEsperado):N0}";
        var periodo = pago.Periodo.ToString("MMMM yyyy", new System.Globalization.CultureInfo("es-AR")).ToUpper();
        var fechaPago = pago.FechaPago.HasValue
            ? pago.FechaPago.Value.ToLocalTime().ToString("dd/MM/yyyy")
            : DateTime.Now.ToString("dd/MM/yyyy");

        container.Column(col =>
        {
            col.Spacing(0);

            // Header empresa
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(config.NombreEmpresa).FontSize(13).Bold().FontColor(AzulOscuro);
                    if (!string.IsNullOrWhiteSpace(config.InfoEmpresa))
                        c.Item().Text(config.InfoEmpresa).FontSize(7).FontColor(Colors.Grey.Darken2);
                });
            });

            col.Item().PaddingTop(4).LineHorizontal(1.5f).LineColor(AzulOscuro);
            col.Item().PaddingTop(8).AlignCenter().Text("RECIBO DE PAGO DE ALQUILER")
                .FontSize(11).Bold().FontColor(AzulOscuro);
            col.Item().PaddingTop(2).AlignCenter().Text($"Período: {periodo}")
                .FontSize(9).Italic().FontColor(Colors.Grey.Darken1);
            col.Item().PaddingTop(6).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);

            // Datos del recibo
            col.Item().PaddingTop(8).Column(datos =>
            {
                FilaRecibo(datos, "Contrato",     contrato.Codigo);
                FilaRecibo(datos, "Cuota N°",     pago.NumeroCuota.ToString());
                FilaRecibo(datos, "Fecha de pago", fechaPago);
            });

            col.Item().PaddingTop(8).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);

            // Datos inmueble y partes
            col.Item().PaddingTop(8).Text("INMUEBLE").FontSize(8).Bold().FontColor(AzulOscuro);
            col.Item().PaddingTop(2).Text(contrato.PropiedadDireccion).FontSize(9);

            col.Item().PaddingTop(6).Text("LOCADOR/A").FontSize(8).Bold().FontColor(AzulOscuro);
            col.Item().PaddingTop(2).Text($"{contrato.LocadorNombre} {contrato.LocadorApellido}" +
                (string.IsNullOrWhiteSpace(contrato.LocadorDni) ? "" : $"  ·  DNI {contrato.LocadorDni}"))
                .FontSize(9);

            col.Item().PaddingTop(6).Text("LOCATARIO/A").FontSize(8).Bold().FontColor(AzulOscuro);
            col.Item().PaddingTop(2).Text($"{contrato.LocatarioNombre} {contrato.LocatarioApellido}" +
                (string.IsNullOrWhiteSpace(contrato.LocatarioDni) ? "" : $"  ·  DNI {contrato.LocatarioDni}"))
                .FontSize(9);

            col.Item().PaddingTop(8).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);

            // Monto destacado
            col.Item().PaddingTop(10).Background("#e8f0fe").Padding(10).Row(row =>
            {
                row.RelativeItem().Text("MONTO ABONADO").FontSize(9).Bold().FontColor(AzulOscuro);
                row.AutoItem().Text(monto).FontSize(13).Bold().FontColor("#1a6e2e");
            });

            col.Item().PaddingTop(6).Column(datos =>
            {
                foreach (var detalle in pago.Detalles)
                {
                    var descripcion = detalle.Medio switch
                    {
                        "Efectivo" => "Efectivo",
                        "Debito"   => "Transferencia / Débito",
                        "Credito"  => "Tarjeta de crédito",
                        "Cheque"   => BuildDescripcionChequeRecibo(detalle),
                        _          => detalle.Medio
                    };
                    if (!string.IsNullOrWhiteSpace(detalle.Referencia) && detalle.Medio != "Cheque")
                        descripcion += $" — {detalle.Referencia}";
                    FilaRecibo(datos, $"{moneda} {detalle.Monto:N0}", descripcion);
                }
                if (!string.IsNullOrWhiteSpace(pago.Observaciones))
                    FilaRecibo(datos, "Observaciones", pago.Observaciones);
            });

            col.Item().PaddingTop(24).Column(firma =>
            {
                firma.Item().Width(160).LineHorizontal(0.5f).LineColor(Colors.Black);
                firma.Item().PaddingTop(4).Text($"Firma del/la LOCADOR/A\n{contrato.LocadorNombre} {contrato.LocadorApellido}")
                    .FontSize(8).FontColor(Colors.Grey.Darken2);
            });

            col.Item().PaddingTop(12).Text(
                "Este recibo cancela el canon locativo correspondiente al período indicado.")
                .FontSize(7.5f).Italic().FontColor(Colors.Grey.Darken1);
        });
    }

    private static void FilaRecibo(ColumnDescriptor col, string etiqueta, string valor)
    {
        col.Item().PaddingBottom(3).Row(row =>
        {
            row.ConstantItem(90).Text(etiqueta + ":").FontSize(8).FontColor(Colors.Grey.Darken2);
            row.RelativeItem().Text(valor).FontSize(8.5f).Bold();
        });
    }

    private static string BuildDescripcionChequeRecibo(PagoDetalleDto d)
    {
        var partes = new System.Text.StringBuilder("Cheque");
        if (!string.IsNullOrWhiteSpace(d.ChequeBanco)) partes.Append($" — {d.ChequeBanco}");
        if (!string.IsNullOrWhiteSpace(d.ChequeNumero)) partes.Append($" N° {d.ChequeNumero}");
        if (d.ChequeFechaVencimiento.HasValue) partes.Append($" — vence {d.ChequeFechaVencimiento.Value:dd/MM/yyyy}");
        return partes.ToString();
    }

    private static void Clausula(ColumnDescriptor col, string numero, string titulo, string texto)
    {
        col.Item().PaddingTop(7).Row(row =>
        {
            row.AutoItem().Text($"{numero}: ").FontSize(8.5f).Bold();
            row.RelativeItem().Text(t =>
            {
                t.Span($"{titulo}. ").Bold().FontSize(8.5f);
                t.Span(texto).FontSize(8.5f);
            });
        });
    }
}
