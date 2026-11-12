
namespace Application.Repositories;

public interface IRepositoryManager
{
    Task SaveAsync(CancellationToken cancellationToken);
    ICorretoraRepository Corretora { get; }
    ICotacaoRepository Cotacao { get; }
    IEmissorRepository Emissor { get; }
    IProdutoRepository Produto { get; }
    IObjetivoRepository Objetivo { get; }
    ITipoRendaRepository TipoRenda { get; }
    IMovimentoRepository Movimento { get; }
}
