namespace Application.Corretoras.Queries.GetCorretora;

public interface IGetCorretoraQuery
{
  Task<CorretoraModel> ExecuteAsync(Guid ownerId, Guid corretoraId);
}
