using Domain.Objetivos;

namespace Persistence.Objetivos;

public interface IObjetivoRepository
{
  Task<IEnumerable<Objetivo>> GetAllObjetivosAsync(Guid ownerId, bool trackChanges);
  Task<Objetivo?> GetObjetivoAsync(Guid ownerId, Guid objetivoId, bool trackChanges);
  Task DeleteObjetivoAsync(Guid ownerId, Guid objetivoId);
  void CreateObjetivo(Objetivo objetivo);
}
