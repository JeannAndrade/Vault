using Domain.Movimentos;

namespace Persistence.Movimentos;

public interface IMovimentoRepository
{
    Task<IEnumerable<Movimento>> GetAllAsync(Guid ownerId, bool trackChanges);
    Task<IEnumerable<Movimento>> GetAllWithRelatedEntitiesAsync(Guid ownerId);
    Task<Movimento?> GetAsync(Guid ownerId, Guid movimentoId, bool trackChanges);
    Task<Movimento?> GetWithRelatedEntitiesAsync(Guid ownerId, Guid movimentoId);
    Task DeleteAsync(Guid ownerId, Guid movimentoId);
    void Create(Movimento movimento);
    void Update(Movimento movimento);
}
