using LumiaFoundation.Abstractions.Pagination;

namespace Presentation.Movimentos;

public interface IMovimentoApi
{
    Task<PagedResponse<MovimentoDto>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken);
    Task<PagedResponse<MovimentoDto>> GetPagedAsync(MovimentoFiltro filtro, int page, int pageSize, CancellationToken cancellationToken);
    Task<List<ProximoVencimentoDto>> GetProximosVencimentosAsync(CancellationToken cancellationToken);
    Task<List<MovimentoDesatualizadoDto>> GetDesatualizadosAsync(CancellationToken cancellationToken);
    Task<MovimentoDto> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<MovimentoDto> CreateAsync(MovimentoForCreationDto movimento, CancellationToken cancellationToken);
    Task UpdateAsync(Guid id, MovimentoForUpdateDto movimento, CancellationToken cancellationToken);
    Task UpdateValorLiquidoAsync(Guid id, MovimentoForUpdateValorLiquidoDto movimento, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
