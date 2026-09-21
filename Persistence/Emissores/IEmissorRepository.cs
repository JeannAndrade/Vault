using Domain.Emissores;

namespace Persistence.Emissores;

public interface IEmissorRepository
{
  Task<IEnumerable<Emissor>> GetAllEmissoresAsync(Guid ownerId, bool trackChanges);
  Task<Emissor?> GetEmissorAsync(Guid ownerId, Guid emissorId, bool trackChanges);
  Task DeleteEmissorAsync(Guid ownerId, Guid emissorId);
  void CreateEmissor(Emissor emissor);
}
