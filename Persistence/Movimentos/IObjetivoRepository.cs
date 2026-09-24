using Domain.Movimentos;

namespace Persistence.Movimentos;

public interface IMovimentoRepository
{
    Task<IEnumerable<Movimento>> GetAllAsync(Guid ownerId, bool trackChanges);
    Task<Movimento?> GetAsync(Guid ownerId, Guid movimentoId, bool trackChanges);
    Task DeleteAsync(Guid ownerId, Guid movimentoId);
    void Create(Movimento movimento);
    void Update(Movimento movimento);
}
