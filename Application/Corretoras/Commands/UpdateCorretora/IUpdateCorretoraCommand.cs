namespace Application.Corretoras.Commands.UpdateCorretora;

public interface IUpdateCorretoraCommand
{
    Task<CorretoraModel> ExecuteAsync(CorretoraModelForUpdate corretoraModel, Guid ownerId, Guid corretoraId);
}
