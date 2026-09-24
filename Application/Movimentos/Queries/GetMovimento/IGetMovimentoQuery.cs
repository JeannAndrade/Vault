namespace Application.Movimentos.Queries.GetMovimento;

public interface IGetMovimentoQuery
{
    Task<MovimentoModel> ExecuteAsync(Guid ownerId, Guid movimentoId);
}
