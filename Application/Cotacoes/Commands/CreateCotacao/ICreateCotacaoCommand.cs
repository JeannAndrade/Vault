namespace Application.Cotacoes.Commands.CreateCotacao;

public interface ICreateCotacaoCommand
{
    Task<CotacaoModel> ExecuteAsync(
        CotacaoModelForCreation model,
        Guid ownerId,
        CancellationToken cancellationToken);
}
