using Domain.TiposRenda;

namespace Persistence.TiposRenda;

public interface ITipoRendaRepository
{
    Task<IEnumerable<TipoRenda>> GetAllAsync(Guid ownerId, bool trackChanges, CancellationToken cancellationToken);
    Task<TipoRenda?> GetAsync(Guid ownerId, Guid tipoRendaId, bool trackChanges, CancellationToken cancellationToken);
    void Delete(TipoRenda tipoRenda);
    void Create(TipoRenda tipoRenda);
    void Update(TipoRenda tipoRenda);
}
