using Domain.Objetivos;

namespace Persistence.Objetivos;

public interface IObjetivoRepository
{
    Task<IEnumerable<Objetivo>> GetAllAsync(Guid ownerId, bool trackChanges);
    Task<Objetivo?> GetAsync(Guid ownerId, Guid objetivoId, bool trackChanges);
    Task<IEnumerable<Objetivo>> GetAllWithRelatedEntitiesAsync(Guid ownerId);
    void Delete(Objetivo objetivo);
    void Create(Objetivo objetivo);
    void Update(Objetivo objetivo);
}
