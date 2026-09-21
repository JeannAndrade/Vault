
using LumiaFoundation.EFRepository.Repository;
using Persistence.Bancos;
using Persistence.Corretoras;
using Persistence.Emissores;
using Persistence.Objetivos;
using Persistence.Produtos;

namespace Persistence.Managment;

public interface IRepositoryManager : IBaseRepositoryManager
{
    IBancoRepository Banco { get; }
    ICorretoraRepository Corretora { get; }
    IEmissorRepository Emissor { get; }
    IProdutoRepository Produto { get; }
    IObjetivoRepository Objetivo { get; }
}
