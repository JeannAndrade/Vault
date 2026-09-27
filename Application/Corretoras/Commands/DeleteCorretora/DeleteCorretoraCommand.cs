using LumiaFoundation.Core.Domain.Exceptions;
using Persistence.Managment;

namespace Application.Corretoras.Commands.DeleteCorretora;

public class DeleteCorretoraCommand(IRepositoryManager repositoryManager) : IDeleteCorretoraCommand
{
    private readonly IRepositoryManager _repositoryManager = repositoryManager;

    public async Task ExecuteAsync(Guid ownerId, Guid corretoraId)
    {
        var corretora = await _repositoryManager.Corretora.GetAsync(ownerId, corretoraId, trackChanges: true)
            ?? throw new EntityNotFoundException("Corretora não encontrada");

        if (await _repositoryManager.Movimento.ExistemPorCorretoraAsync(corretoraId))
            throw new EntityInUseException("Não é possível excluir a Corretora pois existem Movimentos associados a ela.");

        _repositoryManager.Corretora.Delete(corretora);
        await _repositoryManager.SaveAsync();
    }
}
