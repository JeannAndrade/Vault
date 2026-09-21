
using LumiaFoundation.EFRepository.Repository;
using Persistence.Bancos;
using Persistence.Corretoras;
using Persistence.Emissores;

namespace Persistence.Managment;

public interface IRepositoryManager : IBaseRepositoryManager
{
    IBancoRepository Banco { get; }
    ICorretoraRepository Corretora { get; }
    IEmissorRepository Emissor { get; }
}
