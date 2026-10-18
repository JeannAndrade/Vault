using Application.Pagination;
using LumiaFoundation.Core.Pagination;
using LumiaFoundation.Core.Validators;
using Persistence.Managment;

namespace Application.Cotacoes.Queries.GetCotacaoList;

public class GetCotacaoListQuery(IRepositoryManager repositoryManager) : IGetCotacaoListQuery
{
    private readonly IRepositoryManager _repository = repositoryManager;

    public async Task<PagedList<CotacaoModel>> ExecuteAsync(
        Guid ownerId,
        string? papel,
        PaginationParameters paginationParameters,
        CancellationToken cancellationToken)
    {
        CommandValidator.Validate(paginationParameters);

        var papelNormalizado = string.IsNullOrWhiteSpace(papel) ? null : papel.Trim();

        var cotacoes = await _repository.Cotacao.GetPagedAsync(
            ownerId,
            papelNormalizado,
            paginationParameters.Page,
            paginationParameters.PageSize,
            cancellationToken);

        return cotacoes.Map(CotacaoModel.FromDomain);
    }
}
