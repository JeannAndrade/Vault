using LumiaFoundation.Core.Domain.Exceptions;
using Persistence.Managment;

namespace Application.Movimentos.Queries.GetMovimento;

public class GetMovimentoQuery(IRepositoryManager repositoryManager) : IGetMovimentoQuery
{
    private readonly IRepositoryManager _repository = repositoryManager;

    public async Task<MovimentoModel> ExecuteAsync(Guid ownerId, Guid movimentoId)
    {
        var movimento = await _repository.Movimento.GetWithRelatedEntitiesAsync(ownerId, movimentoId)
            ?? throw new EntityNotFoundException("Movimento not found");

        return MovimentoModel.FromDomain(movimento);
    }
}
