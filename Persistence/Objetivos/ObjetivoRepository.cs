using Domain.Objetivos;
using LumiaFoundation.EFRepository.Repository;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Objetivos;

public class ObjetivoRepository(VaultDbContext repositoryContext) : BaseRepository<Objetivo>(repositoryContext), IObjetivoRepository
{
    public async Task<IEnumerable<Objetivo>> GetAllAsync(Guid ownerId, bool trackChanges) =>
        await FindByCondition(c => c.UserId == ownerId, trackChanges).OrderBy(c => c.Nome).ToListAsync();

    public async Task<Objetivo?> GetAsync(Guid ownerId, Guid objetivoId, bool trackChanges) =>
        await FindByCondition(c => c.UserId == ownerId && c.Id == objetivoId, trackChanges).SingleOrDefaultAsync();

    public async Task DeleteAsync(Guid ownerId, Guid objetivoId)
    {
        var objetivo = await GetAsync(ownerId, objetivoId, true);

        if (objetivo is not null)
            Delete(objetivo);
    }
}
