using LumiaFoundation.Core.Domain.Exceptions;
using Persistence.Managment;

namespace Application.Cotacoes.Queries.GetSerieCotacao;

public class GetSerieCotacaoQuery(IRepositoryManager repositoryManager) : IGetSerieCotacaoQuery
{
    private readonly IRepositoryManager _repository = repositoryManager;

    public async Task<List<CotacaoPontoModel>> ExecuteAsync(Guid ownerId, string papel, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(papel))
            throw new CommandValidationException("O papel é obrigatório.");

        var serie = await _repository.Cotacao.GetSerieAsync(ownerId, papel.Trim(), cancellationToken);

        return serie
            .Select(cotacao => new CotacaoPontoModel(cotacao.Data, cotacao.Valor))
            .ToList();
    }
}
