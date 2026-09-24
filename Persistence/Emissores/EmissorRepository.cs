using Domain.Emissores;
using LumiaFoundation.EFRepository.Repository;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Emissores;

public class EmissorRepository(VaultDbContext repositoryContext) : BaseRepository<Emissor>(repositoryContext), IEmissorRepository
{
    public async Task<IEnumerable<Emissor>> GetAllAsync(Guid ownerId, bool trackChanges) =>
        await FindByCondition(c => c.UserId == ownerId, trackChanges).OrderBy(c => c.Nome).ToListAsync();

    public async Task<Emissor?> GetAsync(Guid ownerId, Guid emissorId, bool trackChanges) =>
        await FindByCondition(c => c.UserId == ownerId && c.Id == emissorId, trackChanges).SingleOrDefaultAsync();

    public async Task DeleteAsync(Guid ownerId, Guid emissorId)
    {
        var emissor = await GetAsync(ownerId, emissorId, false);

        if (emissor is not null)
            Delete(emissor);
    }
}
