using Application.Movimentos.Validation;
using LumiaFoundation.Core.Domain.Exceptions;
using LumiaFoundation.Core.Validators;
using Persistence.Managment;

namespace Application.Movimentos.Commands.UpdateMovimento;

public class UpdateMovimentoCommand(
    IRepositoryManager repositoryManager,
    IMovimentoReferenciasValidator referenciasValidator) : IUpdateMovimentoCommand
{
    private readonly IRepositoryManager _repositoryManager = repositoryManager;
    private readonly IMovimentoReferenciasValidator _referenciasValidator = referenciasValidator;

    public async Task<MovimentoModel> ExecuteAsync(MovimentoModelForUpdate movimentoModel, Guid ownerId, Guid movimentoId, CancellationToken cancellationToken)
    {
        CommandValidator.Validate(movimentoModel);

        await _referenciasValidator.ValidarAsync(
            ownerId,
            movimentoModel.ObjetivoId,
            movimentoModel.TipoRendaId,
            movimentoModel.CorretoraId,
            movimentoModel.ProdutoId,
            movimentoModel.EmissorId,
            cancellationToken);

        var movimento = await _repositoryManager.Movimento.GetAsync(ownerId, movimentoId, trackChanges: true, cancellationToken)
            ?? throw new EntityNotFoundException("Movimento not found");

        _repositoryManager.Movimento.Update(movimentoModel.UpdateDomain(movimento));
        await _repositoryManager.SaveAsync(cancellationToken);

        return MovimentoModel.FromDomain(movimento);
    }
}
