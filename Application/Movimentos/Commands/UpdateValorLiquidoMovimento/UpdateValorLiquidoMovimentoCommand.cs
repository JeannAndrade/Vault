using LumiaFoundation.Core.Domain.Exceptions;
using LumiaFoundation.Core.TimeService;
using LumiaFoundation.Core.Validators;
using Persistence.Managment;

namespace Application.Movimentos.Commands.UpdateValorLiquidoMovimento;

public class UpdateValorLiquidoMovimentoCommand(
    IRepositoryManager repositoryManager,
    IDateTimeProvider dateTimeProvider) : IUpdateValorLiquidoMovimentoCommand
{
    private readonly IRepositoryManager _repositoryManager = repositoryManager;
    private readonly IDateTimeProvider _dateTimeProvider = dateTimeProvider;

    public async Task ExecuteAsync(
        MovimentoModelForUpdateValorLiquido model,
        Guid ownerId,
        Guid movimentoId,
        CancellationToken cancellationToken)
    {
        CommandValidator.Validate(model);

        var movimento = await _repositoryManager.Movimento.GetAsync(
            ownerId, movimentoId, trackChanges: true, cancellationToken)
            ?? throw new EntityNotFoundException("Movimento não encontrado");

        model.UpdateDomain(movimento, _dateTimeProvider.GetDateTime().Date);
        _repositoryManager.Movimento.Update(movimento);
        await _repositoryManager.SaveAsync(cancellationToken);
    }
}
