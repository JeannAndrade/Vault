namespace Application.Movimentos.Validation;

public interface IMovimentoReferenciasValidator
{
    Task ValidarAsync(
        Guid ownerId,
        Guid objetivoId,
        Guid tipoRendaId,
        Guid corretoraId,
        Guid produtoId,
        Guid emissorId);
}
