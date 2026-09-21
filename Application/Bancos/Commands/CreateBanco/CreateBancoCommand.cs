using LumiaFoundation.Core.Validators;
using Persistence.Managment;

namespace Application.Bancos.Commands.CreateBanco;

public class CreateBancoCommand(IRepositoryManager repositoryManager) : ICreateBancoCommand
{
    private readonly IRepositoryManager _repositoryManager = repositoryManager;

    public async Task<BancoModel> ExecuteAsync(BancoModelForCreation bancoModel)
    {
        CommandValidator.Validate(bancoModel);

        var banco = bancoModel.ToDomain();
        _repositoryManager.Banco.CreateBanco(banco);
        await _repositoryManager.SaveAsync();

        return BancoModel.FromDomain(banco);
    }
}
