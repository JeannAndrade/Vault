using Domain.Objetivos;
using LumiaFoundation.EFRepository.Repository;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Objetivos;

public class ObjetivoRepository(VaultDbContext repositoryContext) : BaseRepository<Objetivo>(repositoryContext), IObjetivoRepository
{
  public void CreateObjetivo(Objetivo objetivo) => Create(objetivo);

  public async Task<IEnumerable<Objetivo>> GetAllObjetivosAsync(Guid ownerId, bool trackChanges) =>
      await FindByCondition(c => c.UserId == ownerId, trackChanges).OrderBy(c => c.Nome).ToListAsync();

  public async Task<Objetivo?> GetObjetivoAsync(Guid ownerId, Guid objetivoId, bool trackChanges) =>
      await FindByCondition(c => c.UserId == ownerId && c.Id == objetivoId, trackChanges).SingleOrDefaultAsync();

  public async Task DeleteObjetivoAsync(Guid ownerId, Guid objetivoId)
  {
    var objetivo = await GetObjetivoAsync(ownerId, objetivoId, false);

    if (objetivo is not null)
      Delete(objetivo);
  }
}
