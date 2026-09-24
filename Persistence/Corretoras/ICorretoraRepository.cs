using Domain.Corretoras;

namespace Persistence.Corretoras;

public interface ICorretoraRepository
{
    Task<IEnumerable<Corretora>> GetAllAsync(Guid ownerId, bool trackChanges);
    Task<Corretora?> GetAsync(Guid ownerId, Guid corretoraId, bool trackChanges);
    Task DeleteAsync(Guid ownerId, Guid corretoraId);
    void Create(Corretora corretora);
    void Update(Corretora corretora);
}
