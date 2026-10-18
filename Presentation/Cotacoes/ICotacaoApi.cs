using LumiaFoundation.Abstractions.Pagination;

namespace Presentation.Cotacoes;

public interface ICotacaoApi
{
    Task<PagedResponse<CotacaoDto>> GetPagedAsync(string? papel, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<List<string>> GetPapeisAsync(CancellationToken cancellationToken = default);
    Task<List<CotacaoPontoDto>> GetSerieAsync(string papel, CancellationToken cancellationToken = default);
    Task<CotacaoDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CotacaoDto> CreateAsync(CotacaoForCreationDto cotacao, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, CotacaoForUpdateDto cotacao, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
