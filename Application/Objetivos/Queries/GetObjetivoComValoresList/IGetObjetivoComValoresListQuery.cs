namespace Application.Objetivos.Queries.GetObjetivoComValoresList;

public interface IGetObjetivoComValoresListQuery
{
    Task<List<ObjetivoComValoresModel>> ExecuteAsync(Guid ownerId);
}
