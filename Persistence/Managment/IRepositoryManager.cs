
using LumiaFoundation.EFRepository.Repository;
using Persistence.Corretoras;
using Persistence.Emissores;
using Persistence.Objetivos;
using Persistence.Produtos;
using Persistence.TiposRenda;

namespace Persistence.Managment;

public interface IRepositoryManager : IBaseRepositoryManager
{
    ICorretoraRepository Corretora { get; }
    IEmissorRepository Emissor { get; }
    IProdutoRepository Produto { get; }
    IObjetivoRepository Objetivo { get; }
    ITipoRendaRepository TipoRenda { get; }
}
