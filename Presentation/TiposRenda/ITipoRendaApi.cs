namespace Presentation.TiposRenda;

public interface ITipoRendaApi
{
  Task<List<TipoRendaDto>> GetAllAsync(CancellationToken cancellationToken = default);
  Task<TipoRendaDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
  Task<TipoRendaDto> CreateAsync(TipoRendaForCreationDto tipoRenda, CancellationToken cancellationToken = default);
  Task UpdateAsync(Guid id, TipoRendaForUpdateDto tipoRenda, CancellationToken cancellationToken = default);
  Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
