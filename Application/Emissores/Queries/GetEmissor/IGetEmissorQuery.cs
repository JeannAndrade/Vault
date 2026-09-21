namespace Application.Emissores.Queries.GetEmissor;

public interface IGetEmissorQuery
{
  Task<EmissorModel> ExecuteAsync(Guid ownerId, Guid emissorId);
}
