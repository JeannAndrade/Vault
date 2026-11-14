namespace Application.Objetivos.Commands.CreateObjetivo;

public interface ICreateObjetivoCommand
{
  Task<ObjetivoModel> ExecuteAsync(ObjetivoModelForCreation objetivoModel, Guid ownerId, CancellationToken cancellationToken);
}
