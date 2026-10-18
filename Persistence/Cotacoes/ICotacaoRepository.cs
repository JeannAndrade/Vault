using Domain.Cotacoes;
using LumiaFoundation.Core.Pagination;

namespace Persistence.Cotacoes;

public interface ICotacaoRepository
{
    Task<PagedList<Cotacao>> GetPagedAsync(Guid ownerId, string? papel, int page, int pageSize, CancellationToken cancellationToken);
    Task<Cotacao?> GetAsync(Guid ownerId, Guid cotacaoId, bool trackChanges, CancellationToken cancellationToken);
    Task<Cotacao?> GetPorPapelEDataAsync(Guid ownerId, string papel, DateTime data, bool trackChanges, CancellationToken cancellationToken);
    Task<List<string>> GetPapeisAsync(Guid ownerId, CancellationToken cancellationToken);
    Task<List<Cotacao>> GetSerieAsync(Guid ownerId, string papel, CancellationToken cancellationToken);
    Task<List<Cotacao>> GetUltimasPorPapelAsync(Guid ownerId, IReadOnlyCollection<string> papeis, CancellationToken cancellationToken);
    Task<DateTime?> GetDataMaisRecenteAsync(Guid ownerId, string papel, CancellationToken cancellationToken);
    void Create(Cotacao cotacao);
    void Update(Cotacao cotacao);
    void Delete(Cotacao cotacao);
}
