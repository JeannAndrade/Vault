namespace Presentation.Objetivos;

public interface IObjetivoApi
{
    Task<List<ObjetivoDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<List<ResumoObjetivoDto>> GetAllComValoresAsync(CancellationToken cancellationToken);
    Task<ObjetivoDto> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<ObjetivoDto> CreateAsync(ObjetivoForCreationDto objetivo, CancellationToken cancellationToken);
    Task UpdateAsync(Guid id, ObjetivoForUpdateDto objetivo, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
