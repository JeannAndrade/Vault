using Domain.Corretoras;

namespace Persistence.Corretoras;

public interface ICorretoraRepository
{
    Task<IEnumerable<Corretora>> GetAllAsync(Guid ownerId, bool trackChanges, CancellationToken cancellationToken);
    Task<Corretora?> GetAsync(Guid ownerId, Guid corretoraId, bool trackChanges, CancellationToken cancellationToken);
    void Delete(Corretora corretora);
    void Create(Corretora corretora);
    void Update(Corretora corretora);
}
