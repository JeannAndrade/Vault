namespace Application.Emissores.Commands.DeleteEmissor;

public interface IDeleteEmissorCommand
{
    Task ExecuteAsync(Guid ownerId, Guid emissorId);
}
