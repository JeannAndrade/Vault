namespace Presentation.Corretoras;

public interface ICorretoraApi
{
    Task<List<CorretoraDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<CorretoraDto> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<CorretoraDto> CreateAsync(CorretoraForCreationDto corretora, CancellationToken cancellationToken);
    Task UpdateAsync(Guid id, CorretoraForUpdateDto corretora, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
