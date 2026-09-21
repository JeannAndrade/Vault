namespace Application.Objetivos.Queries.GetObjetivoList;

public interface IGetObjetivosListQuery
{
  Task<List<ObjetivoModel>> ExecuteAsync(Guid ownerId);
}
