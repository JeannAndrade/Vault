using Domain.Movimentos;
using LumiaFoundation.Core.Pagination;
using LumiaFoundation.EFRepository.Extensions;
using LumiaFoundation.EFRepository.Repository;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Movimentos;

public class MovimentoRepository(VaultDbContext repositoryContext) : BaseRepository<Movimento>(repositoryContext), IMovimentoRepository
{
    public async Task<IEnumerable<Movimento>> GetAllAsync(Guid ownerId, bool trackChanges) =>
        await FindByCondition(c => c.UserId == ownerId, trackChanges).OrderBy(c => c.DataInvestimento).ToListAsync();

    // Ordem determinística: DataInvestimento decrescente e Id como desempate. Sem o desempate,
    // movimentos na mesma data podem repetir ou sumir entre páginas.
    public async Task<PagedList<Movimento>> GetPagedWithRelatedEntitiesAsync(
        Guid ownerId, int page, int pageSize, CancellationToken cancellationToken = default) =>
        await QueryWithRelatedEntities()
            .Where(m => m.UserId == ownerId)
            .OrderByDescending(m => m.DataInvestimento)
            .ThenBy(m => m.Id)
            .ToPagedListAsync(page, pageSize, cancellationToken);

    public async Task<Movimento?> GetAsync(Guid ownerId, Guid movimentoId, bool trackChanges) =>
        await FindByCondition(c => c.UserId == ownerId && c.Id == movimentoId, trackChanges).SingleOrDefaultAsync();

    public async Task<Movimento?> GetWithRelatedEntitiesAsync(Guid ownerId, Guid movimentoId) =>
        await QueryWithRelatedEntities()
            .SingleOrDefaultAsync(m => m.UserId == ownerId && m.Id == movimentoId);

    public async Task<bool> ExistemPorObjetivoAsync(Guid objetivoId) =>
        await FindByCondition(m => m.ObjetivoId == objetivoId, trackChanges: false).AnyAsync();

    public async Task<bool> ExistemPorTipoRendaAsync(Guid tipoRendaId) =>
        await FindByCondition(m => m.TipoRendaId == tipoRendaId, trackChanges: false).AnyAsync();

    public async Task<bool> ExistemPorCorretoraAsync(Guid corretoraId) =>
        await FindByCondition(m => m.CorretoraId == corretoraId, trackChanges: false).AnyAsync();

    public async Task<bool> ExistemPorProdutoAsync(Guid produtoId) =>
        await FindByCondition(m => m.ProdutoId == produtoId, trackChanges: false).AnyAsync();

    public async Task<bool> ExistemPorEmissorAsync(Guid emissorId) =>
        await FindByCondition(m => m.EmissorId == emissorId, trackChanges: false).AnyAsync();

    private IQueryable<Movimento> QueryWithRelatedEntities() =>
        RepositoryContext.Set<Movimento>()
            .AsNoTracking()
            .Include(m => m.Objetivo)
            .Include(m => m.TipoRenda)
            .Include(m => m.Corretora)
            .Include(m => m.Produto)
            .Include(m => m.Emissor);
}
