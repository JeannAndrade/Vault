using Domain.TiposRenda;
using LumiaFoundation.EFRepository.Repository;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.TiposRenda;

public class TipoRendaRepository(VaultDbContext repositoryContext) : BaseRepository<TipoRenda>(repositoryContext), ITipoRendaRepository
{
  public void CreateTipoRenda(TipoRenda tipoRenda) => Create(tipoRenda);

  public async Task<IEnumerable<TipoRenda>> GetAllTiposRendaAsync(Guid ownerId, bool trackChanges) =>
      await FindByCondition(c => c.UserId == ownerId, trackChanges).OrderBy(c => c.Nome).ToListAsync();

  public async Task<TipoRenda?> GetTipoRendaAsync(Guid ownerId, Guid tipoRendaId, bool trackChanges) =>
      await FindByCondition(c => c.UserId == ownerId && c.Id == tipoRendaId, trackChanges).SingleOrDefaultAsync();

  public async Task DeleteTipoRendaAsync(Guid ownerId, Guid tipoRendaId)
  {
    var tipoRenda = await GetTipoRendaAsync(ownerId, tipoRendaId, false);

    if (tipoRenda is not null)
      Delete(tipoRenda);
  }
}
