namespace Application.Objetivos.Commands.UpdateObjetivo;

public interface IUpdateObjetivoCommand
{
  Task<ObjetivoModel> ExecuteAsync(ObjetivoModelForUpdate objetivoModel, Guid ownerId, Guid objetivoId);
}
