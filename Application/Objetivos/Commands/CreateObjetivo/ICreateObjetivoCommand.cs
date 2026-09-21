namespace Application.Objetivos.Commands.CreateObjetivo;

public interface ICreateObjetivoCommand
{
  Task<ObjetivoModel> ExecuteAsync(ObjetivoModelForCreation objetivoModel);
}
