using LumiaFoundation.Core.Domain.Exceptions;
using Persistence.Managment;

namespace Application.TiposRenda.Commands.DeleteTipoRenda;

public class DeleteTipoRendaCommand(IRepositoryManager repositoryManager) : IDeleteTipoRendaCommand
{
    private readonly IRepositoryManager _repositoryManager = repositoryManager;

    public async Task ExecuteAsync(Guid ownerId, Guid tipoRendaId)
    {
        var tipoRenda = await _repositoryManager.TipoRenda.GetAsync(ownerId, tipoRendaId, trackChanges: true)
            ?? throw new EntityNotFoundException("Tipo de renda não encontrado");

        if (await _repositoryManager.Movimento.ExistemPorTipoRendaAsync(tipoRendaId))
            throw new EntityInUseException("Não é possível excluir o Tipo de Renda pois existem Movimentos associados a ele.");

        _repositoryManager.TipoRenda.Delete(tipoRenda);
        await _repositoryManager.SaveAsync();
    }
}
