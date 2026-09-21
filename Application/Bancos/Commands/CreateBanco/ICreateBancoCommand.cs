namespace Application.Bancos.Commands.CreateBanco;

public interface ICreateBancoCommand
{
    Task<BancoModel> ExecuteAsync(BancoModelForCreation bancoModel);
}
