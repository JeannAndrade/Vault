using LumiaFoundation.Core.Validators;
using Persistence.Management;

namespace Application.Corretoras.Commands.CreateCorretora;

public class CreateCorretoraCommand(IRepositoryManager repositoryManager) : ICreateCorretoraCommand
{
    private readonly IRepositoryManager _repositoryManager = repositoryManager;

    public async Task<CorretoraModel> ExecuteAsync(CorretoraModelForCreation corretoraModel, Guid ownerId, CancellationToken cancellationToken)
    {
        CommandValidator.Validate(corretoraModel);

        var corretora = corretoraModel.ToDomain(ownerId);
        _repositoryManager.Corretora.Create(corretora);
        await _repositoryManager.SaveAsync(cancellationToken);

        return CorretoraModel.FromDomain(corretora);
    }
}
