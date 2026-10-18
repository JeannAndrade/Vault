using LumiaFoundation.Core.Domain.Exceptions;
using Persistence.Managment;

namespace Application.Cotacoes.Queries.GetCotacao;

public class GetCotacaoQuery(IRepositoryManager repositoryManager) : IGetCotacaoQuery
{
    private readonly IRepositoryManager _repository = repositoryManager;

    public async Task<CotacaoModel> ExecuteAsync(Guid ownerId, Guid cotacaoId, CancellationToken cancellationToken)
    {
        var cotacao = await _repository.Cotacao.GetAsync(ownerId, cotacaoId, trackChanges: false, cancellationToken)
            ?? throw new EntityNotFoundException("Cotação não encontrada");

        return CotacaoModel.FromDomain(cotacao);
    }
}
