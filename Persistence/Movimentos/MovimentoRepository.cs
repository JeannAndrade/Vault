using Domain.Movimentos;
using LumiaFoundation.Core.Pagination;
using LumiaFoundation.EFRepository.Extensions;
using LumiaFoundation.EFRepository.Repository;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Movimentos;

public class MovimentoRepository(VaultDbContext repositoryContext) : BaseRepository<Movimento>(repositoryContext), IMovimentoRepository
{
    public async Task<IEnumerable<Movimento>> GetAllAsync(Guid ownerId, bool trackChanges) =>
        await FindByCondition(c => c.UserId == ownerId, trackChanges).OrderBy(c => c.DataInvestimento).ToListAsync();

    // A ordem é toda descendente (DataInvestimento e Id) para o banco percorrer o índice
    // IX_Movimentos_UserId_DataInvestimento (UserId, DataInvestimento e, implicitamente, a PK)
    // de trás para a frente, sem ordenar em memória. Mudar a direção ou o critério desfaz
    // esse ganho: ver ADR 0009. O Id é só desempate; basta ser determinístico.
    public async Task<PagedList<Movimento>> GetPagedWithRelatedEntitiesAsync(
        Guid ownerId, int page, int pageSize, CancellationToken cancellationToken = default) =>
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
        Guid ownerId, int quantidade = 15, CancellationToken cancellationToken = default) =>
        await QueryWithRelatedEntities()
            .Where(m => m.UserId == ownerId && m.EstaAtivo && m.DataVencimento != null)
            .OrderBy(m => m.DataVencimento)
            .ThenBy(m => m.Id)
            .Take(quantidade)
            .ToListAsync(cancellationToken);

    public async Task<List<Movimento>> GetDesatualizadosAsync(
        Guid ownerId, DateTime dataLimite, int quantidade, CancellationToken cancellationToken) =>
        await QueryWithRelatedEntities()
            .Where(m => m.UserId == ownerId && m.EstaAtivo && m.DataAtualizacaoValorLiquido < dataLimite)
            .OrderBy(m => m.DataAtualizacaoValorLiquido)
            .ThenBy(m => m.Id)
            .Take(quantidade)
            .ToListAsync(cancellationToken);

    public async Task<Movimento?> GetAsync(Guid ownerId, Guid movimentoId, bool trackChanges) =>
        await FindByCondition(c => c.UserId == ownerId && c.Id == movimentoId, trackChanges).SingleOrDefaultAsync();

    public async Task<Movimento?> GetAsync(
        Guid ownerId, Guid movimentoId, bool trackChanges, CancellationToken cancellationToken) =>
        await FindByCondition(c => c.UserId == ownerId && c.Id == movimentoId, trackChanges)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<Movimento?> GetWithRelatedEntitiesAsync(Guid ownerId, Guid movimentoId) =>
        await QueryWithRelatedEntities()
            .SingleOrDefaultAsync(m => m.UserId == ownerId && m.Id == movimentoId);

    public async Task<List<Movimento>> GetAtivosPorTituloAsync(
        Guid ownerId,
        TituloFiltro titulo,
        bool trackChanges,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(titulo);

        if (titulo.TipoRendaId == Guid.Empty || titulo.CorretoraId == Guid.Empty ||
            titulo.ProdutoId == Guid.Empty || titulo.EmissorId == Guid.Empty)
            return [];

        // A ordem serve apenas para uma listagem estável. O calculador do Domain
        // reordena em memória para definir o aporte mais recente sem depender
        // da ordenação de Guid do provedor de banco.
        return await FindByCondition(
                movimento => movimento.UserId == ownerId &&
                    movimento.EstaAtivo &&
                    movimento.TipoRendaId == titulo.TipoRendaId &&
                    movimento.CorretoraId == titulo.CorretoraId &&
                    movimento.ProdutoId == titulo.ProdutoId &&
                    movimento.EmissorId == titulo.EmissorId,
                trackChanges)
            .Include(movimento => movimento.Objetivo)
            .OrderBy(movimento => movimento.DataInvestimento)
            .ThenBy(movimento => movimento.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<List<Movimento>> GetAtivosDoTituloAsync(Guid ownerId, MovimentoTituloFiltro titulo, CancellationToken cancellationToken) =>
        ConsultarAtivosDoTitulo(ownerId, titulo, false).Include(m => m.Objetivo).ToListAsync(cancellationToken);

    public Task<List<Movimento>> GetAtivosDoTituloParaAtualizacaoAsync(Guid ownerId, MovimentoTituloFiltro titulo, CancellationToken cancellationToken) =>
        ConsultarAtivosDoTitulo(ownerId, titulo, true).ToListAsync(cancellationToken);

    private IQueryable<Movimento> ConsultarAtivosDoTitulo(Guid ownerId, MovimentoTituloFiltro titulo, bool rastrear) =>
        FindByCondition(m => m.UserId == ownerId && m.EstaAtivo && m.TipoRendaId == titulo.TipoRendaId &&
            m.CorretoraId == titulo.CorretoraId && m.ProdutoId == titulo.ProdutoId && m.EmissorId == titulo.EmissorId, rastrear)
            .OrderBy(m => m.DataInvestimento).ThenBy(m => m.Id);

    public async Task<bool> ExistemPorObjetivoAsync(Guid objetivoId) =>
        await FindByCondition(m => m.ObjetivoId == objetivoId, trackChanges: false).AnyAsync();

    public async Task<bool> ExistemPorTipoRendaAsync(Guid tipoRendaId) =>
        await FindByCondition(m => m.TipoRendaId == tipoRendaId, trackChanges: false).AnyAsync();

    public async Task<bool> ExistemPorCorretoraAsync(Guid corretoraId) =>
        await FindByCondition(m => m.CorretoraId == corretoraId, trackChanges: false).AnyAsync();

    public async Task<bool> ExistemPorProdutoAsync(Guid produtoId) =>
        await FindByCondition(m => m.ProdutoId == produtoId, trackChanges: false).AnyAsync();

    public async Task<bool> ExistemPorEmissorAsync(Guid emissorId) =>
        await FindByCondition(m => m.EmissorId == emissorId, trackChanges: false).AnyAsync();

    private IQueryable<Movimento> QueryWithRelatedEntities() =>
        RepositoryContext.Set<Movimento>()
            .AsNoTracking()
            .Include(m => m.Objetivo)
            .Include(m => m.TipoRenda)
            .Include(m => m.Corretora)
            .Include(m => m.Produto)
            .Include(m => m.Emissor);
}
