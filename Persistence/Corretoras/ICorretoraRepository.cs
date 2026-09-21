using Domain.Corretoras;

namespace Persistence.Corretoras;

public interface ICorretoraRepository
{
  Task<IEnumerable<Corretora>> GetAllCorretorasAsync(Guid ownerId, bool trackChanges);
  Task<Corretora?> GetCorretoraAsync(Guid ownerId, Guid corretoraId, bool trackChanges);
  Task DeleteCorretoraAsync(Guid ownerId, Guid corretoraId);
  void CreateCorretora(Corretora corretora);
}
