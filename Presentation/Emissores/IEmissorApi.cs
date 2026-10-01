namespace Presentation.Emissores;

public interface IEmissorApi
{
  Task<List<EmissorDto>> GetAllAsync(CancellationToken cancellationToken = default);
  Task<EmissorDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
  Task<EmissorDto> CreateAsync(EmissorForCreationDto emissor, CancellationToken cancellationToken = default);
  Task UpdateAsync(Guid id, EmissorForUpdateDto emissor, CancellationToken cancellationToken = default);
  Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
