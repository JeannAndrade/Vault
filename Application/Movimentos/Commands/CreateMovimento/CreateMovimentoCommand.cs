using Application.Movimentos.Validation;
using LumiaFoundation.Core.TimeService;
using LumiaFoundation.Core.Validators;
using Application.Repositories;

namespace Application.Movimentos.Commands.CreateMovimento;

public class CreateMovimentoCommand(
    IRepositoryManager repositoryManager,
    IMovimentoReferenciasValidator referenciasValidator,
    IDateTimeProvider dateTimeProvider) : ICreateMovimentoCommand
{
    private readonly IRepositoryManager _repositoryManager = repositoryManager;
    private readonly IMovimentoReferenciasValidator _referenciasValidator = referenciasValidator;
    private readonly IDateTimeProvider _dateTimeProvider = dateTimeProvider;

    public async Task<MovimentoModel> ExecuteAsync(MovimentoModelForCreation movimentoModel, Guid ownerId, CancellationToken cancellationToken)
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

        var hoje = _dateTimeProvider.GetDateTime().Date;
        var movimento = movimentoModel.ToDomain(ownerId, hoje);

        if (movimentoModel.MotivoEncerramento.HasValue && movimentoModel.DataEncerramento.HasValue)
        {
            EncerramentoValidador.Validar(movimento.DataInvestimento, movimentoModel.DataEncerramento.Value, hoje);
            movimento.Encerrar(movimentoModel.MotivoEncerramento.Value, movimentoModel.DataEncerramento.Value, hoje);
        }

        _repositoryManager.Movimento.Create(movimento);
        await _repositoryManager.SaveAsync(cancellationToken);

        return MovimentoModel.FromDomain(movimento);
    }
}
