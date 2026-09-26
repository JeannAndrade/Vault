using Application.Movimentos.Validation;
using LumiaFoundation.Core.Validators;
using Persistence.Managment;

namespace Application.Movimentos.Commands.CreateMovimento;

public class CreateMovimentoCommand(
    IRepositoryManager repositoryManager,
    IMovimentoReferenciasValidator referenciasValidator) : ICreateMovimentoCommand
{
    private readonly IRepositoryManager _repositoryManager = repositoryManager;
    private readonly IMovimentoReferenciasValidator _referenciasValidator = referenciasValidator;

    public async Task<MovimentoModel> ExecuteAsync(MovimentoModelForCreation movimentoModel, Guid userId)
    {
        CommandValidator.Validate(movimentoModel);

        await _referenciasValidator.ValidarAsync(
            userId,
            movimentoModel.ObjetivoId,
            movimentoModel.TipoRendaId,
            movimentoModel.CorretoraId,
            movimentoModel.ProdutoId,
            movimentoModel.EmissorId);

        var movimento = movimentoModel.ToDomain(userId);
        _repositoryManager.Movimento.Create(movimento);
        await _repositoryManager.SaveAsync();

        return MovimentoModel.FromDomain(movimento);
    }
}
