using Domain.Emissores;

namespace Persistence.Emissores;

public interface IEmissorRepository
{
    Task<IEnumerable<Emissor>> GetAllAsync(Guid ownerId, bool trackChanges);
    Task<Emissor?> GetAsync(Guid ownerId, Guid emissorId, bool trackChanges);
    void Delete(Emissor emissor);
    void Create(Emissor emissor);
    void Update(Emissor emissor);
}
