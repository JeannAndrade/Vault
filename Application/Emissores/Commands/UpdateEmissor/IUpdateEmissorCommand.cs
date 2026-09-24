namespace Application.Emissores.Commands.UpdateEmissor;

public interface IUpdateEmissorCommand
{
  Task<EmissorModel> ExecuteAsync(EmissorModelForUpdate emissorModel, Guid ownerId, Guid emissorId);
}
