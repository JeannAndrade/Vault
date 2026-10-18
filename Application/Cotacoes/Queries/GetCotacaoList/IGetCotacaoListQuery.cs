using Application.Pagination;
using LumiaFoundation.Core.Pagination;

namespace Application.Cotacoes.Queries.GetCotacaoList;

public interface IGetCotacaoListQuery
{
    Task<PagedList<CotacaoModel>> ExecuteAsync(
        Guid ownerId,
        string? papel,
        PaginationParameters paginationParameters,
        CancellationToken cancellationToken);
}
