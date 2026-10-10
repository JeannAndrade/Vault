namespace Application.Movimentos.Queries.GetProximosVencimentos;

public interface IGetProximosVencimentosQuery
{
    Task<List<ProximoVencimentoModel>> ExecuteAsync(Guid ownerId);
}
