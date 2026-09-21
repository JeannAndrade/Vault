using LumiaFoundation.Core.Domain.Exceptions;
using Persistence.Managment;

namespace Application.Bancos.Queries.GetBanco;


public class GetBancoQuery(IRepositoryManager repositoryManager) : IGetBancoQuery
{
    private readonly IRepositoryManager _repository = repositoryManager;

    public async Task<BancoModel> ExecuteAsync(Guid ownerId, Guid bancoId)
    {

        var banco = await _repository.Banco.GetBancoAsync(ownerId, bancoId, trackChanges: false) ?? throw new EntityNotFoundException("Banco not found");

        return new BancoModel
        {
            Id = banco.Id,
            Nome = banco.Nome,
            UserId = banco.UserId
        };
    }
}
