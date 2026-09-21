using LumiaFoundation.Logger.Contracts;
using Persistence.Managment;

namespace Application.Emissores.Queries.GetEmissorList;

public class GetEmissoresListQuery(IRepositoryManager repositoryManager, ILoggerManager logger) : IGetEmissoresListQuery
{
  private readonly IRepositoryManager _repository = repositoryManager;
  private readonly ILoggerManager _logger = logger;

  public async Task<List<EmissorModel>> ExecuteAsync(Guid ownerId)
  {
    try
    {
      var emissores = await _repository.Emissor.GetAllEmissoresAsync(ownerId, trackChanges: false);

      return [.. emissores.Select(e => new EmissorModel
            {
                Id = e.Id,
                Nome = e.Nome,
                UserId = e.UserId
            })];
    }
    catch (Exception ex)
    {
      _logger.LogError($"Something went wrong in the {nameof(GetEmissoresListQuery)} service method {ex}");
      throw;
    }
  }
}
