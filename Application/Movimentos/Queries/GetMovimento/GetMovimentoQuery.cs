using LumiaFoundation.Core.Domain.Exceptions;
using Persistence.Managment;

namespace Application.Movimentos.Queries.GetMovimento;

public class GetMovimentoQuery(IRepositoryManager repositoryManager) : IGetMovimentoQuery
{
    private readonly IRepositoryManager _repository = repositoryManager;

    public async Task<MovimentoModel> ExecuteAsync(Guid ownerId, Guid movimentoId, CancellationToken cancellationToken)
    {
        var movimento = await _repository.Movimento.GetWithRelatedEntitiesAsync(ownerId, movimentoId, cancellationToken)
            ?? throw new EntityNotFoundException("Movimento não encontrado");

        return MovimentoModel.FromDomain(movimento);
    }
}
