using Domain.Movimentos;
using LumiaFoundation.Core.Pagination;

namespace Persistence.Movimentos;

public interface IMovimentoRepository
{
    Task<IEnumerable<Movimento>> GetAllAsync(Guid ownerId, bool trackChanges);
    Task<PagedList<Movimento>> GetPagedWithRelatedEntitiesAsync(Guid ownerId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedList<Movimento>> GetPagedWithRelatedEntitiesAsync(Guid ownerId, MovimentoFiltro filtro, int page, int pageSize, CancellationToken cancellationToken);
    Task<List<Movimento>> GetProximosVencimentosAsync(Guid ownerId, int quantidade = 15, CancellationToken cancellationToken = default);
    Task<List<Movimento>> GetDesatualizadosAsync(Guid ownerId, DateTime dataLimite, int quantidade, CancellationToken cancellationToken);
    Task<Movimento?> GetAsync(Guid ownerId, Guid movimentoId, bool trackChanges);
    Task<Movimento?> GetAsync(Guid ownerId, Guid movimentoId, bool trackChanges, CancellationToken cancellationToken);
    Task<Movimento?> GetWithRelatedEntitiesAsync(Guid ownerId, Guid movimentoId);
    Task<List<Movimento>> GetAtivosPorTituloAsync(
        Guid ownerId,
        TituloFiltro titulo,
        bool trackChanges,
        CancellationToken cancellationToken);
    Task<List<Movimento>> GetAtivosDoTituloAsync(Guid ownerId, MovimentoTituloFiltro titulo, CancellationToken cancellationToken);
    Task<List<Movimento>> GetAtivosDoTituloParaAtualizacaoAsync(Guid ownerId, MovimentoTituloFiltro titulo, CancellationToken cancellationToken);
    Task<bool> ExistemPorObjetivoAsync(Guid objetivoId);
    Task<bool> ExistemPorTipoRendaAsync(Guid tipoRendaId);
    Task<bool> ExistemPorCorretoraAsync(Guid corretoraId);
    Task<bool> ExistemPorProdutoAsync(Guid produtoId);
    Task<bool> ExistemPorEmissorAsync(Guid emissorId);
    void Create(Movimento movimento);
    void Update(Movimento movimento);
    void Delete(Movimento movimento);
}
