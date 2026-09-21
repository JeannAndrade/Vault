using Domain.Emissores;
using LumiaFoundation.EFRepository.Repository;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Emissores;

public class EmissorRepository(VaultDbContext repositoryContext) : BaseRepository<Emissor>(repositoryContext), IEmissorRepository
{
  public void CreateEmissor(Emissor emissor) => Create(emissor);

  public async Task<IEnumerable<Emissor>> GetAllEmissoresAsync(Guid ownerId, bool trackChanges) =>
      await FindByCondition(c => c.UserId == ownerId, trackChanges).OrderBy(c => c.Nome).ToListAsync();

  public async Task<Emissor?> GetEmissorAsync(Guid ownerId, Guid emissorId, bool trackChanges) =>
      await FindByCondition(c => c.UserId == ownerId && c.Id == emissorId, trackChanges).SingleOrDefaultAsync();

  public async Task DeleteEmissorAsync(Guid ownerId, Guid emissorId)
  {
    var emissor = await GetEmissorAsync(ownerId, emissorId, false);

    if (emissor is not null)
      Delete(emissor);
  }
}
