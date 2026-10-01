namespace Presentation.Objetivos;

public interface IObjetivoApi
{
  Task<List<ObjetivoDto>> GetAllAsync(CancellationToken cancellationToken = default);
  Task<ObjetivoDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
  Task<ObjetivoDto> CreateAsync(ObjetivoForCreationDto objetivo, CancellationToken cancellationToken = default);
  Task UpdateAsync(Guid id, ObjetivoForUpdateDto objetivo, CancellationToken cancellationToken = default);
  Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
