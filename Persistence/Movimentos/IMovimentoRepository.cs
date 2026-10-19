using Domain.Movimentos;
using LumiaFoundation.Core.Pagination;

namespace Persistence.Movimentos;

public interface IMovimentoRepository
{
    Task<List<string>> GetPapeisPorCotacaoAsync(Guid ownerId, CancellationToken cancellationToken);
    Task<List<Movimento>> GetAtivosPorCotacaoDoPapelAsync(Guid ownerId, string papel, CancellationToken cancellationToken);
    Task<IEnumerable<Movimento>> GetAllAsync(Guid ownerId, bool trackChanges, CancellationToken cancellationToken);
    Task<PagedList<Movimento>> GetPagedWithRelatedEntitiesAsync(Guid ownerId, int page, int pageSize, CancellationToken cancellationToken);
    Task<PagedList<Movimento>> GetPagedWithRelatedEntitiesAsync(Guid ownerId, MovimentoFiltro filtro, int page, int pageSize, CancellationToken cancellationToken);
    Task<List<Movimento>> GetProximosVencimentosAsync(Guid ownerId, int quantidade, CancellationToken cancellationToken);
    Task<List<Movimento>> GetDesatualizadosAsync(Guid ownerId, DateTime dataLimite, int quantidade, CancellationToken cancellationToken);
    Task<Movimento?> GetAsync(Guid ownerId, Guid movimentoId, bool trackChanges, CancellationToken cancellationToken);
    Task<Movimento?> GetWithRelatedEntitiesAsync(Guid ownerId, Guid movimentoId, CancellationToken cancellationToken);
    Task<List<Movimento>> GetAtivosDoTituloAsync(Guid ownerId, MovimentoTituloFiltro titulo, CancellationToken cancellationToken);
    Task<List<Movimento>> GetAtivosDoTituloParaAtualizacaoAsync(Guid ownerId, MovimentoTituloFiltro titulo, CancellationToken cancellationToken);
    Task<bool> ExistemPorObjetivoAsync(Guid objetivoId, CancellationToken cancellationToken);
    Task<bool> ExistemPorTipoRendaAsync(Guid tipoRendaId, CancellationToken cancellationToken);
    Task<bool> ExistemPorCorretoraAsync(Guid corretoraId, CancellationToken cancellationToken);
    Task<bool> ExistemPorProdutoAsync(Guid produtoId, CancellationToken cancellationToken);
    Task<bool> ExistemPorEmissorAsync(Guid emissorId, CancellationToken cancellationToken);
    void Create(Movimento movimento);
    void Update(Movimento movimento);
    void Delete(Movimento movimento);
}
