namespace Application.Emissores.Queries.GetEmissorList;

public interface IGetEmissoresListQuery
{
  Task<List<EmissorModel>> ExecuteAsync(Guid ownerId);
}
