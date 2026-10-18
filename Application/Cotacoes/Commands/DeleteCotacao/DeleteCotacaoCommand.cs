using LumiaFoundation.Core.Domain.Exceptions;
using Persistence.Managment;

namespace Application.Cotacoes.Commands.DeleteCotacao;

public class DeleteCotacaoCommand(IRepositoryManager r) : IDeleteCotacaoCommand
{
    public async Task ExecuteAsync(Guid ownerId, Guid id, CancellationToken ct)
    {
        var c = await r.Cotacao.GetAsync(ownerId, id, true, ct)
            ?? throw new EntityNotFoundException("Cotação não encontrada");

        r.Cotacao.Delete(c);
        await r.SaveAsync(ct);
    }
}
