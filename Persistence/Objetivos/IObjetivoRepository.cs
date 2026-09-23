using Domain.Objetivos;

namespace Persistence.Objetivos;

public interface IObjetivoRepository
{
    Task<IEnumerable<Objetivo>> GetAllAsync(Guid ownerId, bool trackChanges);
    Task<Objetivo?> GetAsync(Guid ownerId, Guid objetivoId, bool trackChanges);
    Task DeleteAsync(Guid ownerId, Guid objetivoId);
    void Create(Objetivo objetivo);
}
