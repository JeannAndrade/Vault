namespace Application.Movimentos.Queries.GetMovimentoList;

public interface IGetMovimentosListQuery
{
    Task<List<MovimentoModel>> ExecuteAsync(Guid ownerId);
}
