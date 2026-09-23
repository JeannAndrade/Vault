using Domain.TiposRenda;

namespace Persistence.TiposRenda;

public interface ITipoRendaRepository
{
  Task<IEnumerable<TipoRenda>> GetAllTiposRendaAsync(Guid ownerId, bool trackChanges);
  Task<TipoRenda?> GetTipoRendaAsync(Guid ownerId, Guid tipoRendaId, bool trackChanges);
  Task DeleteTipoRendaAsync(Guid ownerId, Guid tipoRendaId);
  void CreateTipoRenda(TipoRenda tipoRenda);
}
