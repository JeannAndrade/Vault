namespace Application.Cotacoes.Commands.DeleteCotacao;
public interface IDeleteCotacaoCommand { Task ExecuteAsync(Guid ownerId, Guid id, CancellationToken cancellationToken); }
