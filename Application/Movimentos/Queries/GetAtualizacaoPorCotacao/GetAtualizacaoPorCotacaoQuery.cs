using Application.Movimentos.AtualizacaoPorCotacao;
using LumiaFoundation.Logger.Contracts;
using Persistence.Managment;

namespace Application.Movimentos.Queries.GetAtualizacaoPorCotacao;

public class GetAtualizacaoPorCotacaoQuery(
    IRepositoryManager repositoryManager,
    ILoggerManager logger) : IGetAtualizacaoPorCotacaoQuery
{
    private readonly IRepositoryManager _repository = repositoryManager;
    private readonly ILoggerManager _logger = logger;

    public async Task<List<PapelParaCotacaoModel>> ExecuteAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        try
        {
            var papeis = await _repository.Movimento.GetPapeisPorCotacaoAsync(ownerId, cancellationToken);

            if (papeis.Count == 0)
                return [];

            var ultimasCotacoes = await _repository.Cotacao.GetUltimasPorPapelAsync(ownerId, papeis, cancellationToken);

            return papeis
                .Select(papel =>
                {
                    var cotacao = ultimasCotacoes.FirstOrDefault(x =>
                        string.Equals(x.Papel, papel, StringComparison.OrdinalIgnoreCase));

                    return new PapelParaCotacaoModel(papel, cotacao?.Valor, cotacao?.Data);
                })
                .OrderBy(x => x.Papel, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError($"Falha em {nameof(GetAtualizacaoPorCotacaoQuery)}: {ex}");
            throw;
        }
    }
}
