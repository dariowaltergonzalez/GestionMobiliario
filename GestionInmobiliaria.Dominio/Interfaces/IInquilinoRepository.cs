using GestionInmobiliaria.Dominio.Entidades;

namespace GestionInmobiliaria.Dominio.Interfaces;

public interface IInquilinoRepository
{
    Task<IEnumerable<Inquilino>> GetAllAsync(string? buscar = null, bool? activo = null);
    Task<Inquilino?> GetByIdAsync(int id);
    Task<Inquilino> CreateAsync(Inquilino inquilino);
    Task<Inquilino> UpdateAsync(Inquilino inquilino);
    Task<bool> DeleteAsync(int id);
}
