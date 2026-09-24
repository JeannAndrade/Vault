using Domain.TiposRenda;

namespace Persistence.TiposRenda;

public interface ITipoRendaRepository
{
    Task<IEnumerable<TipoRenda>> GetAllAsync(Guid ownerId, bool trackChanges);
    Task<TipoRenda?> GetAsync(Guid ownerId, Guid tipoRendaId, bool trackChanges);
    Task DeleteAsync(Guid ownerId, Guid tipoRendaId);
    void Create(TipoRenda tipoRenda);
    void Update(TipoRenda tipoRenda);
}
