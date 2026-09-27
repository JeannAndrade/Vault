using LumiaFoundation.Core.Domain.Exceptions;
using Persistence.Managment;

namespace Application.Emissores.Commands.DeleteEmissor;

public class DeleteEmissorCommand(IRepositoryManager repositoryManager) : IDeleteEmissorCommand
{
    private readonly IRepositoryManager _repositoryManager = repositoryManager;

    public async Task ExecuteAsync(Guid ownerId, Guid emissorId)
    {
        var emissor = await _repositoryManager.Emissor.GetAsync(ownerId, emissorId, trackChanges: true)
            ?? throw new EntityNotFoundException("Emissor não encontrado");

        if (await _repositoryManager.Movimento.ExistemPorEmissorAsync(emissorId))
            throw new EntityInUseException("Não é possível excluir o Emissor pois existem Movimentos associados a ele.");

        _repositoryManager.Emissor.Delete(emissor);
        await _repositoryManager.SaveAsync();
    }
}
