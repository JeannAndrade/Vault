using Domain.Movimentos;
using LumiaFoundation.Core.Pagination;
using LumiaFoundation.EFRepository.Extensions;
using LumiaFoundation.EFRepository.Repository;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Movimentos;

public class MovimentoRepository(VaultDbContext repositoryContext) : BaseRepository<Movimento>(repositoryContext), IMovimentoRepository
{
    public Task<List<string>> GetPapeisPorCotacaoAsync(Guid ownerId, CancellationToken cancellationToken) =>
        RepositoryContext.Set<Movimento>().AsNoTracking()
            .Where(m => m.UserId == ownerId && m.EstaAtivo && m.AtualizaPorCotacao && m.Quantidade != null)
            .Select(m => m.Emissor.Nome).Distinct().OrderBy(nome => nome).ToListAsync(cancellationToken);

    public Task<List<Movimento>> GetAtivosPorCotacaoDoPapelAsync(Guid ownerId, string papel, CancellationToken cancellationToken) =>
        FindByCondition(m => m.UserId == ownerId && m.EstaAtivo && m.AtualizaPorCotacao &&
            m.Quantidade != null && m.Emissor.Nome == papel, true).ToListAsync(cancellationToken);

    public async Task<IEnumerable<Movimento>> GetAllAsync(Guid ownerId, bool trackChanges, CancellationToken cancellationToken) =>
        await FindByCondition(c => c.UserId == ownerId, trackChanges).OrderBy(c => c.DataInvestimento).ToListAsync(cancellationToken);

    // A ordem é toda descendente (DataInvestimento e Id) para o banco percorrer o índice
    // IX_Movimentos_UserId_DataInvestimento (UserId, DataInvestimento e, implicitamente, a PK)
    // de trás para a frente, sem ordenar em memória. Mudar a direção ou o critério desfaz
    // esse ganho: ver ADR 0009. O Id é só desempate; basta ser determinístico.
    public async Task<PagedList<Movimento>> GetPagedWithRelatedEntitiesAsync(
        Guid ownerId, int page, int pageSize, CancellationToken cancellationToken) =>
        await GetPagedWithRelatedEntitiesAsync(ownerId, MovimentoFiltro.Vazio, page, pageSize, cancellationToken);

    public async Task<PagedList<Movimento>> GetPagedWithRelatedEntitiesAsync(
        Guid ownerId, MovimentoFiltro filtro, int page, int pageSize, CancellationToken cancellationToken)
    {
        filtro ??= MovimentoFiltro.Vazio;

        var query = QueryWithRelatedEntities()
            .Where(m => m.UserId == ownerId);

        if (filtro.ObjetivoId.HasValue)
            query = query.Where(m => m.ObjetivoId == filtro.ObjetivoId.Value);

        if (filtro.TipoRendaId.HasValue)
            query = query.Where(m => m.TipoRendaId == filtro.TipoRendaId.Value);

        if (filtro.CorretoraId.HasValue)
            query = query.Where(m => m.CorretoraId == filtro.CorretoraId.Value);

        if (filtro.ProdutoId.HasValue)
            query = query.Where(m => m.ProdutoId == filtro.ProdutoId.Value);

        if (filtro.EmissorId.HasValue)
            query = query.Where(m => m.EmissorId == filtro.EmissorId.Value);

        if (filtro.EstaAtivo.HasValue)
            query = query.Where(m => m.EstaAtivo == filtro.EstaAtivo.Value);

        return await query
            .OrderByDescending(m => m.DataInvestimento)
            .ThenByDescending(m => m.Id)
            .ToPagedListAsync(page, pageSize, cancellationToken);
    }

    public async Task<List<Movimento>> GetProximosVencimentosAsync(
        Guid ownerId, int quantidade, CancellationToken cancellationToken) =>
        await QueryWithRelatedEntities()
            .Where(m => m.UserId == ownerId && m.EstaAtivo && m.DataVencimento != null)
            .OrderBy(m => m.DataVencimento)
            .ThenBy(m => m.Id)
            .Take(quantidade)
            .ToListAsync(cancellationToken);

    // A ordem dos tipos define a ordem dos grupos na tela; ver ADR 0018 (E2).
    public Task<List<Movimento>> GetAtivosParaRankingAsync(Guid ownerId, CancellationToken cancellationToken) =>
        QueryWithRelatedEntities()
            .Where(m => m.UserId == ownerId && m.EstaAtivo)
            .OrderBy(m => m.TipoRenda.Nome)
            .ThenBy(m => m.TipoRendaId)
            .ToListAsync(cancellationToken);

    // O limite superior é exclusivo e não há limite inferior de propósito: vencidos ativos somam no mês atual (ADR 0019).
    public Task<List<RetornoPrevistoItem>> GetRetornoPrevistoAsync(Guid ownerId, DateTime limiteSuperior, CancellationToken cancellationToken) =>
        FindByCondition(m => m.UserId == ownerId && m.EstaAtivo && m.DataVencimento != null && m.DataVencimento < limiteSuperior, false)
            .Select(m => new RetornoPrevistoItem(m.DataVencimento!.Value, m.ValorLiquidoAtual))
            .ToListAsync(cancellationToken);

    // A ausência de EstaAtivo é intencional: o histórico de aportes não some com o resgate (ADR 0019).
    public Task<List<AporteInvestidoItem>> GetInvestidoAsync(Guid ownerId, DateTime inicio, DateTime fim, CancellationToken cancellationToken) =>
        FindByCondition(m => m.UserId == ownerId && m.DataInvestimento >= inicio && m.DataInvestimento < fim, false)
            .Select(m => new AporteInvestidoItem(m.DataInvestimento, m.ValorAporte, m.EhReinvestimento))
            .ToListAsync(cancellationToken);

    // A ordem agrupa por título para a manutenção do valor líquido: Corretora, Produto e Emissor (por nome,
    // ascendente), com DataAtualizacaoValorLiquido e Id como desempate determinístico. O índice
    // IX_Movimentos_UserId_EstaAtivo_DataAtualizacaoValorLiquido atende apenas o filtro; a ordenação
    // é feita pelo banco sobre o conjunto filtrado do usuário (filesort): ver ADR 0015.
    public async Task<List<Movimento>> GetDesatualizadosAsync(
        Guid ownerId, DateTime dataLimite, int quantidade, CancellationToken cancellationToken) =>
        await QueryWithRelatedEntities()
            .Where(m => m.UserId == ownerId && m.EstaAtivo && !m.AtualizaPorCotacao && m.DataAtualizacaoValorLiquido < dataLimite)
            .OrderBy(m => m.Corretora.Nome)
            .ThenBy(m => m.Produto.Nome)
            .ThenBy(m => m.Emissor.Nome)
            .ThenBy(m => m.DataAtualizacaoValorLiquido)
            .ThenBy(m => m.Id)
            .Take(quantidade)
            .ToListAsync(cancellationToken);

    public async Task<Movimento?> GetAsync(
        Guid ownerId, Guid movimentoId, bool trackChanges, CancellationToken cancellationToken) =>
        await FindByCondition(c => c.UserId == ownerId && c.Id == movimentoId, trackChanges)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<Movimento?> GetWithRelatedEntitiesAsync(Guid ownerId, Guid movimentoId, CancellationToken cancellationToken) =>
        await QueryWithRelatedEntities()
            .SingleOrDefaultAsync(m => m.UserId == ownerId && m.Id == movimentoId, cancellationToken);

    public Task<List<Movimento>> GetAtivosDoTituloAsync(Guid ownerId, MovimentoTituloFiltro titulo, CancellationToken cancellationToken) =>
        ConsultarAtivosDoTitulo(ownerId, titulo, false).Include(m => m.Objetivo).ToListAsync(cancellationToken);

    public Task<List<Movimento>> GetAtivosDoTituloParaAtualizacaoAsync(Guid ownerId, MovimentoTituloFiltro titulo, CancellationToken cancellationToken) =>
        ConsultarAtivosDoTitulo(ownerId, titulo, true).ToListAsync(cancellationToken);

    private IQueryable<Movimento> ConsultarAtivosDoTitulo(Guid ownerId, MovimentoTituloFiltro titulo, bool rastrear) =>
        FindByCondition(m => m.UserId == ownerId && m.EstaAtivo && !m.AtualizaPorCotacao && m.TipoRendaId == titulo.TipoRendaId &&
            m.CorretoraId == titulo.CorretoraId && m.ProdutoId == titulo.ProdutoId && m.EmissorId == titulo.EmissorId, rastrear)
            .OrderBy(m => m.DataInvestimento).ThenBy(m => m.Id);

    public async Task<bool> ExistemPorObjetivoAsync(Guid objetivoId, CancellationToken cancellationToken) =>
        await FindByCondition(m => m.ObjetivoId == objetivoId, trackChanges: false).AnyAsync(cancellationToken);

    public async Task<bool> ExistemPorTipoRendaAsync(Guid tipoRendaId, CancellationToken cancellationToken) =>
        await FindByCondition(m => m.TipoRendaId == tipoRendaId, trackChanges: false).AnyAsync(cancellationToken);

    public async Task<bool> ExistemPorCorretoraAsync(Guid corretoraId, CancellationToken cancellationToken) =>
        await FindByCondition(m => m.CorretoraId == corretoraId, trackChanges: false).AnyAsync(cancellationToken);

    public async Task<bool> ExistemPorProdutoAsync(Guid produtoId, CancellationToken cancellationToken) =>
        await FindByCondition(m => m.ProdutoId == produtoId, trackChanges: false).AnyAsync(cancellationToken);

    public async Task<bool> ExistemPorEmissorAsync(Guid emissorId, CancellationToken cancellationToken) =>
        await FindByCondition(m => m.EmissorId == emissorId, trackChanges: false).AnyAsync(cancellationToken);

    private IQueryable<Movimento> QueryWithRelatedEntities() =>
        RepositoryContext.Set<Movimento>()
            .AsNoTracking()
            .Include(m => m.Objetivo)
            .Include(m => m.TipoRenda)
            .Include(m => m.Corretora)
            .Include(m => m.Produto)
            .Include(m => m.Emissor);
}
