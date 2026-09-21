using Domain.Bancos;

namespace Persistence.Bancos;

public interface IBancoRepository
{
    Task<IEnumerable<Banco>> GetAllBancosAsync(Guid ownerId, bool trackChanges);
    Task<Banco?> GetBancoAsync(Guid ownerId, Guid bancoId, bool trackChanges);
    Task DeleteBancoAsync(Guid ownerId, Guid bancoId);
    void CreateBanco(Banco banco);
}
