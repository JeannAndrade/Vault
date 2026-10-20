namespace Presentation.TiposRenda;

public interface ITipoRendaApi
{
  Task<List<TipoRendaDto>> GetAllAsync(CancellationToken cancellationToken);
  Task<TipoRendaDto> GetByIdAsync(Guid id, CancellationToken cancellationToken);
  Task<TipoRendaDto> CreateAsync(TipoRendaForCreationDto tipoRenda, CancellationToken cancellationToken);
  Task UpdateAsync(Guid id, TipoRendaForUpdateDto tipoRenda, CancellationToken cancellationToken);
  Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
