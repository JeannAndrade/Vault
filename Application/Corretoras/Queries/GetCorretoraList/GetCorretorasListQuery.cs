using LumiaFoundation.Logger.Contracts;
using Persistence.Managment;

namespace Application.Corretoras.Queries.GetCorretoraList;

public class GetCorretorasListQuery(IRepositoryManager repositoryManager, ILoggerManager logger) : IGetCorretorasListQuery
{
  private readonly IRepositoryManager _repository = repositoryManager;
  private readonly ILoggerManager _logger = logger;

  public async Task<List<CorretoraModel>> ExecuteAsync(Guid ownerId)
  {
    try
    {
      var corretoras = await _repository.Corretora.GetAllAsync(ownerId, trackChanges: false);

      return [.. corretoras.Select(c => new CorretoraModel
            {
                Id = c.Id,
                Nome = c.Nome,
                UserId = c.UserId
            })];
    }
    catch (Exception ex)
    {
      _logger.LogError($"Something went wrong in the {nameof(GetCorretorasListQuery)} service method {ex}");
      throw;
    }
  }
}
