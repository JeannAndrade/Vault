using Application.Pagination;
using LumiaFoundation.Core.Pagination;
using LumiaFoundation.Core.Validators;
using Application.Repositories;

namespace Application.Movimentos.Queries.GetMovimentoList;

public class GetMovimentosListQuery(IRepositoryManager repositoryManager) : IGetMovimentosListQuery
{
    private readonly IRepositoryManager _repository = repositoryManager;

    public Task<PagedList<MovimentoModel>> ExecuteAsync(
        Guid ownerId, PaginationParameters parameters, CancellationToken cancellationToken) =>
        ExecuteAsync(ownerId, new MovimentoFiltroParameters(), parameters, cancellationToken);

    public async Task<PagedList<MovimentoModel>> ExecuteAsync(
        Guid ownerId, MovimentoFiltroParameters filtro, PaginationParameters paginacao, CancellationToken cancellationToken)
    {
        // Fora do try: parâmetro inválido é erro do cliente (422), não falha do serviço a logar como erro.
        CommandValidator.Validate(paginacao);

            var movimentos = await _repository.Movimento.GetPagedWithRelatedEntitiesAsync(
                ownerId, filtro.ToMovimentoFiltro(), paginacao.Page, paginacao.PageSize, cancellationToken);

            return movimentos.Map(MovimentoModel.FromDomain);

    }
}
