namespace Application.Emissores.Commands.CreateEmissor;

public interface ICreateEmissorCommand
{
  Task<EmissorModel> ExecuteAsync(EmissorModelForCreation emissorModel);
}
