namespace Application.TiposRenda.Queries.GetTipoRenda;

public interface IGetTipoRendaQuery
{
  Task<TipoRendaModel> ExecuteAsync(Guid ownerId, Guid tipoRendaId);
}
