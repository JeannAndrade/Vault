using Domain.Objetivos;
using LumiaFoundation.EFRepository.Repository;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Objetivos;

public class ObjetivoRepository(VaultDbContext repositoryContext) : BaseRepository<Objetivo>(repositoryContext), IObjetivoRepository
{
    public async Task<IEnumerable<Objetivo>> GetAllAsync(Guid ownerId, bool trackChanges, CancellationToken cancellationToken) =>
        await FindByCondition(c => c.UserId == ownerId, trackChanges).OrderBy(c => c.Nome).ToListAsync(cancellationToken);

    public async Task<IEnumerable<Objetivo>> GetAllWithRelatedEntitiesAsync(Guid ownerId, CancellationToken cancellationToken) =>
        await QueryWithRelatedEntities()
            .Where(m => m.UserId == ownerId)
            .OrderBy(m => m.Nome)
            .ToListAsync(cancellationToken);

    public async Task<Objetivo?> GetAsync(Guid ownerId, Guid objetivoId, bool trackChanges, CancellationToken cancellationToken) =>
        await FindByCondition(c => c.UserId == ownerId && c.Id == objetivoId, trackChanges).SingleOrDefaultAsync(cancellationToken);

    public async Task DeleteAsync(Guid ownerId, Guid objetivoId, CancellationToken cancellationToken)
    {
        var objetivo = await GetAsync(ownerId, objetivoId, true, cancellationToken);

        if (objetivo is not null)
            Delete(objetivo);
    }

    private IQueryable<Objetivo> QueryWithRelatedEntities() =>
        RepositoryContext.Set<Objetivo>()
            .AsNoTracking()
            .Include(o => o.Movimentos);
}
