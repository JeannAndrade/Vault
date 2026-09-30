namespace Presentation.Corretoras;

public interface ICorretoraApi
{
    Task<List<CorretoraDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<CorretoraDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CorretoraDto> CreateAsync(CorretoraForCreationDto corretora, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, CorretoraForUpdateDto corretora, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
