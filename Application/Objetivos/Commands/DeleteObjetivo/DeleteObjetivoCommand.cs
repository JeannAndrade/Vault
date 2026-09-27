using LumiaFoundation.Core.Domain.Exceptions;
using Persistence.Managment;

namespace Application.Objetivos.Commands.DeleteObjetivo;

public class DeleteObjetivoCommand(IRepositoryManager repositoryManager) : IDeleteObjetivoCommand
{
    private readonly IRepositoryManager _repositoryManager = repositoryManager;

    public async Task ExecuteAsync(Guid ownerId, Guid objetivoId)
    {
        var objetivo = await _repositoryManager.Objetivo.GetAsync(ownerId, objetivoId, trackChanges: true)
            ?? throw new EntityNotFoundException("Objetivo não encontrado");

        if (await _repositoryManager.Movimento.ExistemPorObjetivoAsync(objetivoId))
            throw new EntityInUseException("Não é possível excluir o Objetivo pois existem Movimentos associados a ele.");

        _repositoryManager.Objetivo.Delete(objetivo);
        await _repositoryManager.SaveAsync();
    }
}
