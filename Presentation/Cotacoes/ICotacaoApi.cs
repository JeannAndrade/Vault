using LumiaFoundation.Abstractions.Pagination;

namespace Presentation.Cotacoes;

public interface ICotacaoApi
{
    Task<PagedResponse<CotacaoDto>> GetPagedAsync(string? papel, int page, int pageSize, CancellationToken cancellationToken);
    Task<List<string>> GetPapeisAsync(CancellationToken cancellationToken);
    Task<List<CotacaoPontoDto>> GetSerieAsync(string papel, CancellationToken cancellationToken);
    Task<CotacaoDto> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<CotacaoDto> CreateAsync(CotacaoForCreationDto cotacao, CancellationToken cancellationToken);
    Task UpdateAsync(Guid id, CotacaoForUpdateDto cotacao, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
