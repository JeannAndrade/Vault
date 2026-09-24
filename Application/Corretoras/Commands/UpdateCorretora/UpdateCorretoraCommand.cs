using LumiaFoundation.Core.Domain.Exceptions;
using LumiaFoundation.Core.Validators;
using Persistence.Managment;

namespace Application.Corretoras.Commands.UpdateCorretora;

public class UpdateCorretoraCommand(IRepositoryManager repositoryManager) : IUpdateCorretoraCommand
{
    private readonly IRepositoryManager _repositoryManager = repositoryManager;

    public async Task<CorretoraModel> ExecuteAsync(CorretoraModelForUpdate corretoraModel, Guid ownerId, Guid corretoraId)
    {
        CommandValidator.Validate(corretoraModel);

        var corretora = await _repositoryManager.Corretora.GetAsync(ownerId, corretoraId, trackChanges: true)
        ?? throw new EntityNotFoundException("Corretora not found");

        _repositoryManager.Corretora.Update(corretoraModel.UpdateDomain(corretora));
        await _repositoryManager.SaveAsync();

        return CorretoraModel.FromDomain(corretora);
    }
}
