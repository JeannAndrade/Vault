namespace Application.Corretoras.Commands.DeleteCorretora;

public interface IDeleteCorretoraCommand
{
    Task ExecuteAsync(Guid ownerId, Guid corretoraId);
}
