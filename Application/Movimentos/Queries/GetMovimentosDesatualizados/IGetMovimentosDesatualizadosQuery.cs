namespace Application.Movimentos.Queries.GetMovimentosDesatualizados;

public interface IGetMovimentosDesatualizadosQuery
{
    Task<List<MovimentoDesatualizadoModel>> ExecuteAsync(Guid ownerId, CancellationToken cancellationToken);
}
