namespace Presentation.Emissores;

public interface IEmissorApi
{
  Task<List<EmissorDto>> GetAllAsync(CancellationToken cancellationToken);
  Task<EmissorDto> GetByIdAsync(Guid id, CancellationToken cancellationToken);
  Task<EmissorDto> CreateAsync(EmissorForCreationDto emissor, CancellationToken cancellationToken);
  Task UpdateAsync(Guid id, EmissorForUpdateDto emissor, CancellationToken cancellationToken);
  Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
