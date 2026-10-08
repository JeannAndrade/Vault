using Application.Pagination;
using LumiaFoundation.Core.Pagination;
using LumiaFoundation.Core.Validators;
using LumiaFoundation.Logger.Contracts;
using Persistence.Managment;

namespace Application.Movimentos.Queries.GetMovimentoList;

public class GetMovimentosListQuery(IRepositoryManager repositoryManager, ILoggerManager logger) : IGetMovimentosListQuery
{
    private readonly IRepositoryManager _repository = repositoryManager;
    private readonly ILoggerManager _logger = logger;

    public async Task<PagedList<MovimentoModel>> ExecuteAsync(
        Guid ownerId, PaginationParameters parameters, CancellationToken cancellationToken = default)
    {
        // Fora do try: parâmetro inválido é erro do cliente (422), não falha do serviço a logar como erro.
        CommandValidator.Validate(parameters);

        try
        {
            var movimentos = await _repository.Movimento.GetPagedWithRelatedEntitiesAsync(
                ownerId, parameters.Page, parameters.PageSize, cancellationToken);

            return movimentos.Map(MovimentoModel.FromDomain);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError($"Algo deu errado no método de serviço {nameof(GetMovimentosListQuery)}: {ex}");
            throw;
        }
    }
}
