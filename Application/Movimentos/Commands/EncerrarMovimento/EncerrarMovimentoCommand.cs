using Application.Movimentos.Validation;
using LumiaFoundation.Core.Domain.Exceptions;
using LumiaFoundation.Core.TimeService;
using LumiaFoundation.Core.Validators;
using Persistence.Managment;

namespace Application.Movimentos.Commands.EncerrarMovimento;

public class EncerrarMovimentoCommand(
    IRepositoryManager repositoryManager,
    IDateTimeProvider dateTimeProvider) : IEncerrarMovimentoCommand
{
    private readonly IRepositoryManager _repositoryManager = repositoryManager;
    private readonly IDateTimeProvider _dateTimeProvider = dateTimeProvider;

    public async Task ExecuteAsync(
        MovimentoModelForEncerramento model,
        Guid ownerId,
        Guid movimentoId,
        CancellationToken cancellationToken)
    {
        CommandValidator.Validate(model);

        var movimento = await _repositoryManager.Movimento.GetAsync(
            ownerId,
            movimentoId,
            trackChanges: true,
            cancellationToken)
            ?? throw new EntityNotFoundException("Movimento não encontrado");

        var hoje = _dateTimeProvider.GetDateTime().Date;
        EncerramentoValidador.Validar(movimento.DataInvestimento, model.Data!.Value, hoje);

        movimento.Encerrar(model.Motivo!.Value, model.Data.Value, hoje);
        _repositoryManager.Movimento.Update(movimento);
        await _repositoryManager.SaveAsync(cancellationToken);
    }
}
