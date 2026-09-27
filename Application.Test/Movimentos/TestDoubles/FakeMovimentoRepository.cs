using Domain.Movimentos;
using Persistence.Movimentos;

namespace Application.Test.Movimentos.TestDoubles;

internal sealed class FakeMovimentoRepository : IMovimentoRepository
{
    public bool CreateChamado { get; private set; }
    public bool UpdateChamado { get; private set; }
    public Movimento? MovimentoParaRetornar { get; set; }

    public Task<IEnumerable<Movimento>> GetAllAsync(Guid ownerId, bool trackChanges) => throw new NotSupportedException();
    public Task<IEnumerable<Movimento>> GetAllWithRelatedEntitiesAsync(Guid ownerId) => throw new NotSupportedException();
    public Task<Movimento?> GetAsync(Guid ownerId, Guid movimentoId, bool trackChanges) => Task.FromResult(MovimentoParaRetornar);
    public Task<Movimento?> GetWithRelatedEntitiesAsync(Guid ownerId, Guid movimentoId) => throw new NotSupportedException();
    public Task DeleteAsync(Guid ownerId, Guid movimentoId) => throw new NotSupportedException();
    public void Create(Movimento movimento) => CreateChamado = true;
    public void Update(Movimento movimento) => UpdateChamado = true;

    public Task<bool> ExistemPorObjetivoAsync(Guid objetivoId)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ExistemPorTipoRendaAsync(Guid tipoRendaId)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ExistemPorCorretoraAsync(Guid corretoraId)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ExistemPorProdutoAsync(Guid produtoId)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ExistemPorEmissorAsync(Guid emissorId)
    {
        throw new NotImplementedException();
    }

    public void Delete(Movimento movimento)
    {
        throw new NotImplementedException();
    }
}
