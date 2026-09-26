using Domain.Movimentos;
using LumiaFoundation.EFRepository.Repository;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Movimentos;

public class MovimentoRepository(VaultDbContext repositoryContext) : BaseRepository<Movimento>(repositoryContext), IMovimentoRepository
{
    public async Task<IEnumerable<Movimento>> GetAllAsync(Guid ownerId, bool trackChanges) =>
        await FindByCondition(c => c.UserId == ownerId, trackChanges).OrderBy(c => c.DataInvestimento).ToListAsync();

    public async Task<IEnumerable<Movimento>> GetAllWithRelatedEntitiesAsync(Guid ownerId) =>
        await QueryWithRelatedEntities()
            .Where(m => m.UserId == ownerId)
            .OrderBy(m => m.DataInvestimento)
            .ToListAsync();

    public async Task<Movimento?> GetAsync(Guid ownerId, Guid movimentoId, bool trackChanges) =>
        await FindByCondition(c => c.UserId == ownerId && c.Id == movimentoId, trackChanges).SingleOrDefaultAsync();

    public async Task<Movimento?> GetWithRelatedEntitiesAsync(Guid ownerId, Guid movimentoId) =>
        await QueryWithRelatedEntities()
            .SingleOrDefaultAsync(m => m.UserId == ownerId && m.Id == movimentoId);

    public async Task DeleteAsync(Guid ownerId, Guid movimentoId)
    {
        var movimento = await GetAsync(ownerId, movimentoId, true);

        if (movimento is not null)
            Delete(movimento);
    }

    // Somente leitura — usada pelas Queries de apresentação. O fluxo de escrita
    // (Update/Delete) continua em GetAsync, sem pagar o custo dos 5 Includes.
    private IQueryable<Movimento> QueryWithRelatedEntities() =>
        RepositoryContext.Set<Movimento>()
            .AsNoTracking()
            .Include(m => m.Objetivo)
            .Include(m => m.TipoRenda)
            .Include(m => m.Corretora)
            .Include(m => m.Produto)
            .Include(m => m.Emissor);
}
