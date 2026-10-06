namespace Application.Objetivos.Queries.GetResumoObjetivosList;

public interface IGetResumoObjetivosListQuery
{
    Task<List<ResumoObjetivoModel>> ExecuteAsync(Guid ownerId);
}
