using Domain.Emissores;

namespace Application.Repositories;

public interface IEmissorRepository
{
    Task<IEnumerable<Emissor>> GetAllAsync(Guid ownerId, bool trackChanges, CancellationToken cancellationToken);
    Task<Emissor?> GetAsync(Guid ownerId, Guid emissorId, bool trackChanges, CancellationToken cancellationToken);
    void Delete(Emissor emissor);
    void Create(Emissor emissor);
    void Update(Emissor emissor);
}
