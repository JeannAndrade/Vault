using Domain.Movimentos;

namespace Persistence.Movimentos;

public interface IMovimentoRepository
{
    Task<IEnumerable<Movimento>> GetAllAsync(Guid ownerId, bool trackChanges);
    Task<IEnumerable<Movimento>> GetAllWithRelatedEntitiesAsync(Guid ownerId);
    Task<Movimento?> GetAsync(Guid ownerId, Guid movimentoId, bool trackChanges);
    Task<Movimento?> GetWithRelatedEntitiesAsync(Guid ownerId, Guid movimentoId);
    Task<bool> ExistemPorObjetivoAsync(Guid objetivoId);
    Task<bool> ExistemPorTipoRendaAsync(Guid tipoRendaId);
    Task<bool> ExistemPorCorretoraAsync(Guid corretoraId);
    Task<bool> ExistemPorProdutoAsync(Guid produtoId);
    Task<bool> ExistemPorEmissorAsync(Guid emissorId);
    void Create(Movimento movimento);
    void Update(Movimento movimento);
    void Delete(Movimento movimento);
}
