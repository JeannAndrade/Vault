namespace Application.Bancos.Queries.GetBancoList;

public interface IGetBancosListQuery
{
    Task<List<BancoModel>> ExecuteAsync(Guid ownerId);
}
