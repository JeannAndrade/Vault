namespace Application.Corretoras.Commands.CreateCorretora;

public interface ICreateCorretoraCommand
{
  Task<CorretoraModel> ExecuteAsync(CorretoraModelForCreation corretoraModel);
}
