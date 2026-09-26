using Application.Movimentos.Validation;
using LumiaFoundation.Core.Domain.Exceptions;

namespace Application.Test.Movimentos.TestDoubles;

internal sealed class FakeMovimentoReferenciasValidator(bool deveFalhar = false) : IMovimentoReferenciasValidator
{
    public bool Chamado { get; private set; }

    public Task ValidarAsync(Guid ownerId, Guid objetivoId, Guid tipoRendaId, Guid corretoraId, Guid produtoId, Guid emissorId)
    {
        Chamado = true;
        return deveFalhar
            ? throw new EntityNotFoundException("Objetivo not found")
            : Task.CompletedTask;
    }
}
