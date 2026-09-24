using Domain.TiposRenda;
using LumiaFoundation.EFRepository.Repository;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.TiposRenda;

public class TipoRendaRepository(VaultDbContext repositoryContext) : BaseRepository<TipoRenda>(repositoryContext), ITipoRendaRepository
{
    public async Task<IEnumerable<TipoRenda>> GetAllAsync(Guid ownerId, bool trackChanges) =>
        await FindByCondition(c => c.UserId == ownerId, trackChanges).OrderBy(c => c.Nome).ToListAsync();

    public async Task<TipoRenda?> GetAsync(Guid ownerId, Guid tipoRendaId, bool trackChanges) =>
        await FindByCondition(c => c.UserId == ownerId && c.Id == tipoRendaId, trackChanges).SingleOrDefaultAsync();

    public async Task DeleteAsync(Guid ownerId, Guid tipoRendaId)
    {
        var tipoRenda = await GetAsync(ownerId, tipoRendaId, false);

        if (tipoRenda is not null)
            Delete(tipoRenda);
    }
}
