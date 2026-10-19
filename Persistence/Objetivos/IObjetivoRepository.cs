using Domain.Objetivos;

namespace Persistence.Objetivos;

public interface IObjetivoRepository
{
    Task<IEnumerable<Objetivo>> GetAllAsync(Guid ownerId, bool trackChanges, CancellationToken cancellationToken);
    Task<Objetivo?> GetAsync(Guid ownerId, Guid objetivoId, bool trackChanges, CancellationToken cancellationToken);
    Task<IEnumerable<Objetivo>> GetAllWithRelatedEntitiesAsync(Guid ownerId, CancellationToken cancellationToken);
    Task DeleteAsync(Guid ownerId, Guid objetivoId, CancellationToken cancellationToken);
    void Delete(Objetivo objetivo);
    void Create(Objetivo objetivo);
    void Update(Objetivo objetivo);
}
