namespace Application.Objetivos.Commands.DeleteObjetivo;

public interface IDeleteObjetivoCommand
{
    Task ExecuteAsync(Guid ownerId, Guid objetivoId);
}
