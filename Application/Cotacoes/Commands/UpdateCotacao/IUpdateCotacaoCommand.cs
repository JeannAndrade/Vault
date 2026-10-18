namespace Application.Cotacoes.Commands.UpdateCotacao;

public interface IUpdateCotacaoCommand
{
    Task<CotacaoModel> ExecuteAsync(
        CotacaoModelForUpdate model,
        Guid ownerId,
        Guid cotacaoId,
        CancellationToken cancellationToken);
}
