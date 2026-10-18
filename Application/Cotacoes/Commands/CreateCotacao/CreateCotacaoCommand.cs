using LumiaFoundation.Core.Domain.Exceptions;
using LumiaFoundation.Core.TimeService;
using LumiaFoundation.Core.Validators;
using Persistence.Managment;

namespace Application.Cotacoes.Commands.CreateCotacao;

public class CreateCotacaoCommand(IRepositoryManager r, IDateTimeProvider clock) : ICreateCotacaoCommand
{
    public async Task<CotacaoModel> ExecuteAsync(CotacaoModelForCreation m, Guid userId, CancellationToken ct)
    {
        CommandValidator.Validate(m);

        var d = m.Data.Date;

        if (d > clock.GetDateTime().Date)
            throw new CommandValidationException("A data da cotação não pode ser futura.");

        var c = m.ToDomain(userId);

        if (await r.Cotacao.GetPorPapelEDataAsync(userId, c.Papel, d, false, ct) != null)
            throw new CommandValidationException("Já existe cotação para este papel nesta data. Edite o registro existente.");

        r.Cotacao.Create(c);
        await r.SaveAsync(ct);

        return CotacaoModel.FromDomain(c);
    }
}
