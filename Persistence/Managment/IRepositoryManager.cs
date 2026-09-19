
using LumiaFoundation.EFRepository.Repository;
using Persistence.Bancos;

namespace Persistence.Managment;

public interface IRepositoryManager : IBaseRepositoryManager
{
    IBancoRepository Banco { get; }
}
