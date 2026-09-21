namespace Application.Bancos.Queries.GetBanco;

public interface IGetBancoQuery
{
    Task<BancoModel> ExecuteAsync(Guid ownerId, Guid bancoId);
}
