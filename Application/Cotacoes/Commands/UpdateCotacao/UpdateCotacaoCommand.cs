using LumiaFoundation.Core.Domain.Exceptions;
using LumiaFoundation.Core.TimeService;
using LumiaFoundation.Core.Validators;
using Persistence.Managment;

namespace Application.Cotacoes.Commands.UpdateCotacao;

public class UpdateCotacaoCommand(IRepositoryManager r, IDateTimeProvider clock) : IUpdateCotacaoCommand
{
    public async Task<CotacaoModel> ExecuteAsync(CotacaoModelForUpdate m, Guid ownerId, Guid id, CancellationToken ct)
    {
        CommandValidator.Validate(m);

        if (m.Data.Date > clock.GetDateTime().Date)
            throw new CommandValidationException("A data da cotação não pode ser futura.");

        var c = await r.Cotacao.GetAsync(ownerId, id, true, ct)
            ?? throw new EntityNotFoundException("Cotação não encontrada");

        var papel = m.Papel!.Trim();

        var outro = await r.Cotacao.GetPorPapelEDataAsync(ownerId, papel, m.Data.Date, false, ct);

        if (outro != null && outro.Id != id)
            throw new CommandValidationException("Já existe cotação para este papel nesta data. Edite o registro existente.");

        m.UpdateDomain(c);
        r.Cotacao.Update(c);
        await r.SaveAsync(ct);

        return CotacaoModel.FromDomain(c);
    }
}
