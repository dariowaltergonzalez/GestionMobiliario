using GestionInmobiliaria.Aplicacion.DTOs;
using GestionInmobiliaria.Dominio.Entidades;
using GestionInmobiliaria.Infraestructura.Persistencia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionInmobiliaria.WebApi.Controllers;

[ApiController]
[Route("api/configuracion")]
[Authorize]
public class ConfiguracionEmpresaController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ConfiguracionEmpresaController(ApplicationDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var config = await _context.ConfiguracionEmpresa.FirstOrDefaultAsync();
        if (config is null)
            return Ok(ApiResponse<ConfiguracionEmpresaDto>.Ok(new ConfiguracionEmpresaDto()));

        return Ok(ApiResponse<ConfiguracionEmpresaDto>.Ok(MapToDto(config)));
    }

    [HttpPut]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update([FromBody] UpdateConfiguracionEmpresaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NombreComercial))
            return BadRequest(ApiResponse<ConfiguracionEmpresaDto>.Fail("El nombre comercial es requerido."));

        var config = await _context.ConfiguracionEmpresa.FirstOrDefaultAsync();

        if (config is null)
        {
            config = new ConfiguracionEmpresa();
            _context.ConfiguracionEmpresa.Add(config);
        }

        config.NombreComercial = request.NombreComercial;
        config.RazonSocial = request.RazonSocial;
        config.Cuit = request.Cuit;
        config.CondicionFiscal = request.CondicionFiscal;
        config.LogoUrl = request.LogoUrl;
        config.Slogan = request.Slogan;
        config.Telefono = request.Telefono;
        config.WhatsApp = request.WhatsApp;
        config.Email = request.Email;
        config.SitioWeb = request.SitioWeb;
        config.Direccion = request.Direccion;
        config.Ciudad = request.Ciudad;
        config.Provincia = request.Provincia;
        config.CodigoPostal = request.CodigoPostal;
        config.Pais = request.Pais;
        config.Instagram = request.Instagram;
        config.Facebook = request.Facebook;
        config.Twitter = request.Twitter;
        config.FechaActualizacion = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<ConfiguracionEmpresaDto>.Ok(MapToDto(config), "Configuración actualizada correctamente."));
    }

    private static ConfiguracionEmpresaDto MapToDto(ConfiguracionEmpresa c) => new()
    {
        Id = c.Id,
        NombreComercial = c.NombreComercial,
        RazonSocial = c.RazonSocial,
        Cuit = c.Cuit,
        CondicionFiscal = c.CondicionFiscal,
        LogoUrl = c.LogoUrl,
        Slogan = c.Slogan,
        Telefono = c.Telefono,
        WhatsApp = c.WhatsApp,
        Email = c.Email,
        SitioWeb = c.SitioWeb,
        Direccion = c.Direccion,
        Ciudad = c.Ciudad,
        Provincia = c.Provincia,
        CodigoPostal = c.CodigoPostal,
        Pais = c.Pais,
        Instagram = c.Instagram,
        Facebook = c.Facebook,
        Twitter = c.Twitter,
        FechaActualizacion = c.FechaActualizacion
    };
}
