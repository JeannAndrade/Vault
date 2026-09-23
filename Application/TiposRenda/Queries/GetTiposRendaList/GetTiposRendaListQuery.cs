using LumiaFoundation.Logger.Contracts;
using Persistence.Managment;

namespace Application.TiposRenda.Queries.GetTiposRendaList;

public class GetTiposRendaListQuery(IRepositoryManager repositoryManager, ILoggerManager logger) : IGetTiposRendaListQuery
{
  private readonly IRepositoryManager _repository = repositoryManager;
  private readonly ILoggerManager _logger = logger;

  public async Task<List<TipoRendaModel>> ExecuteAsync(Guid ownerId)
  {
    try
    {
      var tiposRenda = await _repository.TipoRenda.GetAllAsync(ownerId, trackChanges: false);

      return [.. tiposRenda.Select(t => new TipoRendaModel
            {
                Id = t.Id,
                Nome = t.Nome,
                UserId = t.UserId
            })];
    }
    catch (Exception ex)
    {
      _logger.LogError($"Something went wrong in the {nameof(GetTiposRendaListQuery)} service method {ex}");
      throw;
    }
  }
}
