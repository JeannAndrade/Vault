namespace Application.Corretoras.Queries.GetCorretoraList;

public interface IGetCorretorasListQuery
{
  Task<List<CorretoraModel>> ExecuteAsync(Guid ownerId);
}
