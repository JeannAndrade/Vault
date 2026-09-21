namespace Application.Objetivos.Queries.GetObjetivo;

public interface IGetObjetivoQuery
{
  Task<ObjetivoModel> ExecuteAsync(Guid ownerId, Guid objetivoId);
}
