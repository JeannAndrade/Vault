using Domain.Movimentos;
using LumiaFoundation.Core.Pagination;
using Persistence.Movimentos;

namespace Application.Test.Movimentos.TestDoubles;

internal sealed class FakeMovimentoRepository : IMovimentoRepository
{
    public Task<List<string>> GetPapeisPorCotacaoAsync(Guid ownerId, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task<List<Movimento>> GetAtivosPorCotacaoDoPapelAsync(Guid ownerId, string papel, CancellationToken cancellationToken) => throw new NotSupportedException();
    public bool CreateChamado { get; private set; }
    public Movimento? MovimentoCriado { get; private set; }
    public bool UpdateChamado { get; private set; }
    public Movimento? MovimentoParaRetornar { get; set; }

    public Task<IEnumerable<Movimento>> GetAllAsync(Guid ownerId, bool trackChanges, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task<PagedList<Movimento>> GetPagedWithRelatedEntitiesAsync(Guid ownerId, int page, int pageSize, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task<PagedList<Movimento>> GetPagedWithRelatedEntitiesAsync(Guid ownerId, MovimentoFiltro filtro, int page, int pageSize, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task<List<Movimento>> GetProximosVencimentosAsync(Guid ownerId, int quantidade, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task<List<Movimento>> GetAtivosParaRankingAsync(Guid ownerId, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task<List<RetornoPrevistoItem>> GetRetornoPrevistoAsync(Guid ownerId, DateTime limiteSuperior, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task<List<AporteInvestidoItem>> GetInvestidoAsync(Guid ownerId, DateTime inicio, DateTime fim, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task<List<Movimento>> GetDesatualizadosAsync(Guid ownerId, DateTime dataLimite, int quantidade, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task<Movimento?> GetAsync(Guid ownerId, Guid movimentoId, bool trackChanges) => Task.FromResult(MovimentoParaRetornar);
    public Task<Movimento?> GetAsync(Guid ownerId, Guid movimentoId, bool trackChanges, CancellationToken cancellationToken) => Task.FromResult(MovimentoParaRetornar);
    public Task<Movimento?> GetWithRelatedEntitiesAsync(Guid ownerId, Guid movimentoId, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task<List<Movimento>> GetAtivosDoTituloAsync(Guid ownerId, MovimentoTituloFiltro titulo, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task<List<Movimento>> GetAtivosDoTituloParaAtualizacaoAsync(Guid ownerId, MovimentoTituloFiltro titulo, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task DeleteAsync(Guid ownerId, Guid movimentoId) => throw new NotSupportedException();
    public void Create(Movimento movimento)
    {
        CreateChamado = true;
        MovimentoCriado = movimento;
    }
    public void Update(Movimento movimento) => UpdateChamado = true;

    public Task<bool> ExistemPorObjetivoAsync(Guid objetivoId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ExistemPorTipoRendaAsync(Guid tipoRendaId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ExistemPorCorretoraAsync(Guid corretoraId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ExistemPorProdutoAsync(Guid produtoId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ExistemPorEmissorAsync(Guid emissorId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public void Delete(Movimento movimento)
    {
        throw new NotImplementedException();
    }
}
