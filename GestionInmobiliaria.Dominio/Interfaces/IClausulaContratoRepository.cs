using GestionInmobiliaria.Dominio.Entidades;

namespace GestionInmobiliaria.Dominio.Interfaces;

public interface IClausulaContratoRepository
{
    Task<IEnumerable<ClausulaContrato>> GetAllAsync();
    Task<IEnumerable<ClausulaContrato>> GetActivasAsync();
    Task<ClausulaContrato?> GetByIdAsync(int id);
    Task<ClausulaContrato> CreateAsync(ClausulaContrato clausula);
    Task<ClausulaContrato> UpdateAsync(ClausulaContrato clausula);
    Task<bool> DeleteAsync(int id);
    Task MoverAsync(int id, bool subir);
    Task InicializarDefaultsAsync();
}
