namespace Application.TiposRenda.Queries.GetTiposRendaList;

public interface IGetTiposRendaListQuery
{
  Task<List<TipoRendaModel>> ExecuteAsync(Guid ownerId);
}
