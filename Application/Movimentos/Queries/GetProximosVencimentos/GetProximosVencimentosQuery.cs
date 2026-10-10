using LumiaFoundation.Logger.Contracts;
using Persistence.Managment;

namespace Application.Movimentos.Queries.GetProximosVencimentos;

public class GetProximosVencimentosQuery(IRepositoryManager repositoryManager, ILoggerManager logger) : IGetProximosVencimentosQuery
{
    private const int QuantidadeMaxima = 15;
    private readonly IRepositoryManager _repository = repositoryManager;
    private readonly ILoggerManager _logger = logger;

    public async Task<List<ProximoVencimentoModel>> ExecuteAsync(Guid ownerId)
    {
        try
        {
            var movimentos = await _repository.Movimento.GetProximosVencimentosAsync(ownerId, QuantidadeMaxima);

            return [.. movimentos.Select(ProximoVencimentoModel.FromDomain)];
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError($"Algo deu errado no método de serviço {nameof(GetProximosVencimentosQuery)}: {ex}");
            throw;
        }
    }
}
