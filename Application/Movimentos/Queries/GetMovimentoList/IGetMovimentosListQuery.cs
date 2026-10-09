using Application.Pagination;
using LumiaFoundation.Core.Pagination;

namespace Application.Movimentos.Queries.GetMovimentoList;

public interface IGetMovimentosListQuery
{
    Task<PagedList<MovimentoModel>> ExecuteAsync(Guid ownerId, PaginationParameters parameters, CancellationToken cancellationToken = default);
}
