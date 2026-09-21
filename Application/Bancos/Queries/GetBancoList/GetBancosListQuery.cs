using LumiaFoundation.Logger.Contracts;
using Persistence.Managment;

namespace Application.Bancos.Queries.GetBancoList;

public class GetBancosListQuery(IRepositoryManager repositoryManager, ILoggerManager logger) : IGetBancosListQuery
{
    private readonly IRepositoryManager _repository = repositoryManager;
    private readonly ILoggerManager _logger = logger;

    public async Task<List<BancoModel>> ExecuteAsync(Guid ownerId)
    {
        try
        {
            var bancos = await _repository.Banco.GetAllBancosAsync(ownerId, trackChanges: false);

            return [.. bancos.Select(b => new BancoModel
            {
                Id = b.Id,
                Nome = b.Nome,
                UserId = b.UserId
            })];
        }
        catch (Exception ex)
        {
            _logger.LogError($"Something went wrong in the {nameof(GetBancosListQuery)} service method {ex}");
            throw;
        }
    }
}
