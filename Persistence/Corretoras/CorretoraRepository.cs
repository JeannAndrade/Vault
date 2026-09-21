using Domain.Corretoras;
using LumiaFoundation.EFRepository.Repository;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Corretoras;

public class CorretoraRepository(VaultDbContext repositoryContext) : BaseRepository<Corretora>(repositoryContext), ICorretoraRepository
{
  public void CreateCorretora(Corretora corretora) => Create(corretora);

  public async Task<IEnumerable<Corretora>> GetAllCorretorasAsync(Guid ownerId, bool trackChanges) =>
      await FindByCondition(c => c.UserId == ownerId, trackChanges).OrderBy(c => c.Nome).ToListAsync();

  public async Task<Corretora?> GetCorretoraAsync(Guid ownerId, Guid corretoraId, bool trackChanges) =>
      await FindByCondition(c => c.UserId == ownerId && c.Id == corretoraId, trackChanges).SingleOrDefaultAsync();

  public async Task DeleteCorretoraAsync(Guid ownerId, Guid corretoraId)
  {
    var corretora = await GetCorretoraAsync(ownerId, corretoraId, false);

    if (corretora is not null)
      Delete(corretora);
  }
}
