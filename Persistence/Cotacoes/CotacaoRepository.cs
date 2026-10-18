using Domain.Cotacoes;
using LumiaFoundation.Core.Pagination;
using LumiaFoundation.EFRepository.Extensions;
using LumiaFoundation.EFRepository.Repository;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Cotacoes;

public class CotacaoRepository(VaultDbContext repositoryContext) : BaseRepository<Cotacao>(repositoryContext), ICotacaoRepository
{
    public Task<PagedList<Cotacao>> GetPagedAsync(Guid ownerId, string? papel, int page, int pageSize, CancellationToken cancellationToken) =>
        FindByCondition(c => c.UserId == ownerId && (papel == null || c.Papel == papel), false)
            .OrderByDescending(c => c.Data).ThenBy(c => c.Papel).ThenBy(c => c.Id)
            .ToPagedListAsync(page, pageSize, cancellationToken);

    public Task<Cotacao?> GetAsync(Guid ownerId, Guid cotacaoId, bool trackChanges, CancellationToken cancellationToken) =>
        FindByCondition(c => c.UserId == ownerId && c.Id == cotacaoId, trackChanges).SingleOrDefaultAsync(cancellationToken);

    public Task<Cotacao?> GetPorPapelEDataAsync(Guid ownerId, string papel, DateTime data, bool trackChanges, CancellationToken cancellationToken) =>
        FindByCondition(c => c.UserId == ownerId && c.Papel == papel && c.Data == data.Date, trackChanges).SingleOrDefaultAsync(cancellationToken);

    public Task<List<string>> GetPapeisAsync(Guid ownerId, CancellationToken cancellationToken) =>
        FindByCondition(c => c.UserId == ownerId, false).Select(c => c.Papel).Distinct().OrderBy(p => p).ToListAsync(cancellationToken);

    public Task<List<Cotacao>> GetSerieAsync(Guid ownerId, string papel, CancellationToken cancellationToken) =>
        FindByCondition(c => c.UserId == ownerId && c.Papel == papel, false).OrderBy(c => c.Data).ThenBy(c => c.Id).ToListAsync(cancellationToken);

    public Task<List<Cotacao>> GetUltimasPorPapelAsync(Guid ownerId, IReadOnlyCollection<string> papeis, CancellationToken cancellationToken) =>
        FindByCondition(c => c.UserId == ownerId && papeis.Contains(c.Papel) &&
            c.Data == RepositoryContext.Set<Cotacao>().Where(x => x.UserId == ownerId && x.Papel == c.Papel).Max(x => x.Data), false)
            .ToListAsync(cancellationToken);

    public Task<DateTime?> GetDataMaisRecenteAsync(Guid ownerId, string papel, CancellationToken cancellationToken) =>
        FindByCondition(c => c.UserId == ownerId && c.Papel == papel, false)
            .Select(c => (DateTime?)c.Data).MaxAsync(cancellationToken);

}
