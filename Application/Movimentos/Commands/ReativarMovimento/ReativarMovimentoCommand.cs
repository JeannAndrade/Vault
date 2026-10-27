using LumiaFoundation.Core.Domain.Exceptions;
using Persistence.Managment;

namespace Application.Movimentos.Commands.ReativarMovimento;

public class ReativarMovimentoCommand(IRepositoryManager repositoryManager) : IReativarMovimentoCommand
{
    private readonly IRepositoryManager _repositoryManager = repositoryManager;

    public async Task ExecuteAsync(Guid ownerId, Guid movimentoId, CancellationToken cancellationToken)
    {
        var movimento = await _repositoryManager.Movimento.GetAsync(
            ownerId,
            movimentoId,
            trackChanges: true,
            cancellationToken)
            ?? throw new EntityNotFoundException("Movimento não encontrado");

        movimento.Reativar();
        _repositoryManager.Movimento.Update(movimento);
        await _repositoryManager.SaveAsync(cancellationToken);
    }
}
