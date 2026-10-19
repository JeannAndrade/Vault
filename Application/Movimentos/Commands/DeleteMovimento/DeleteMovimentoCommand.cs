using LumiaFoundation.Core.Domain.Exceptions;
using Persistence.Managment;

namespace Application.Movimentos.Commands.DeleteMovimento;

public class DeleteMovimentoCommand(IRepositoryManager repositoryManager) : IDeleteMovimentoCommand
{
    private readonly IRepositoryManager _repositoryManager = repositoryManager;

    public async Task ExecuteAsync(Guid ownerId, Guid movimentoId, CancellationToken cancellationToken)
    {
        var movimento = await _repositoryManager.Movimento.GetAsync(ownerId, movimentoId, trackChanges: true, cancellationToken)
            ?? throw new EntityNotFoundException("Movimento não encontrado");

        _repositoryManager.Movimento.Delete(movimento);
        await _repositoryManager.SaveAsync(cancellationToken);
    }
}
