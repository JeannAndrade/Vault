using LumiaFoundation.Abstractions.Pagination;

namespace Presentation.Movimentos;

public interface IMovimentoApi
{
    Task<PagedResponse<MovimentoDto>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<List<ProximoVencimentoDto>> GetProximosVencimentosAsync(CancellationToken cancellationToken = default);
    Task<MovimentoDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<MovimentoDto> CreateAsync(MovimentoForCreationDto movimento, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, MovimentoForUpdateDto movimento, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
