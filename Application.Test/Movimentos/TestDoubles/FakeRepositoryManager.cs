using Persistence.Corretoras;
using Persistence.Emissores;
using Persistence.Managment;
using Persistence.Movimentos;
using Persistence.Objetivos;
using Persistence.Produtos;
using Persistence.TiposRenda;

namespace Application.Test.Movimentos.TestDoubles;

internal sealed class FakeRepositoryManager : IRepositoryManager
{
    public FakeMovimentoRepository MovimentoRepository { get; } = new();
    public bool SaveAsyncChamado { get; private set; }

    public ICorretoraRepository Corretora => throw new NotSupportedException();
    public IEmissorRepository Emissor => throw new NotSupportedException();
    public IProdutoRepository Produto => throw new NotSupportedException();
    public IObjetivoRepository Objetivo => throw new NotSupportedException();
    public ITipoRendaRepository TipoRenda => throw new NotSupportedException();
    public IMovimentoRepository Movimento => MovimentoRepository;

    public Task SaveAsync()
    {
        SaveAsyncChamado = true;
        return Task.CompletedTask;
    }
}
