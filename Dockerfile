# Build multi-etapa: la imagen final no lleva el SDK, solo el runtime — mucho más liviana.
# Usamos .NET 10 (todavía SDK preview) porque el proyecto está targeteado a net10.0 — ver
# docs/logica-negocio.md, sección PENDIENTES GENERALES → "Desplegar el sistema".

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiamos primero solo los .csproj para aprovechar el cache de capas de Docker: si el código
# cambia pero las dependencias no, "dotnet restore" no se vuelve a ejecutar.
COPY GestionInmobiliaria.Dominio/*.csproj GestionInmobiliaria.Dominio/
COPY GestionInmobiliaria.Aplicacion/*.csproj GestionInmobiliaria.Aplicacion/
COPY GestionInmobiliaria.Infraestructura/*.csproj GestionInmobiliaria.Infraestructura/
COPY GestionInmobiliaria.WebApi/*.csproj GestionInmobiliaria.WebApi/
RUN dotnet restore GestionInmobiliaria.WebApi/GestionInmobiliaria.WebApi.csproj

COPY GestionInmobiliaria.Dominio/ GestionInmobiliaria.Dominio/
COPY GestionInmobiliaria.Aplicacion/ GestionInmobiliaria.Aplicacion/
COPY GestionInmobiliaria.Infraestructura/ GestionInmobiliaria.Infraestructura/
COPY GestionInmobiliaria.WebApi/ GestionInmobiliaria.WebApi/

RUN dotnet publish GestionInmobiliaria.WebApi/GestionInmobiliaria.WebApi.csproj \
    -c Release -o /app --no-restore

# Imagen final: solo el runtime de ASP.NET, sin el SDK completo.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# libfontconfig1: dependencia nativa que QuestPDF (generación de PDFs de reportes/recibos)
# necesita en Linux para el manejo de fuentes, aunque se presente como "100% .NET puro" — problema
# ya conocido de versiones anteriores en Azure Linux (ver memoria del proyecto, pdf_reportes.md).
# Si igual falla en Render, el plan de contingencia es migrar a PdfSharpCore (IPdfReportService ya
# está diseñado para ese swap sin tocar el resto del sistema).
RUN apt-get update && apt-get install -y --no-install-recommends libfontconfig1 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app .

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

# Render inyecta la variable PORT en tiempo de ejecución; si no está (ej. build/test local),
# usamos 8080 como default. CMD en forma "shell" (no array) para que $PORT se expanda.
CMD ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet GestionInmobiliaria.WebApi.dll
