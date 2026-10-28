using LumiaFoundation.Core.Domain.Exceptions;

namespace Application.Movimentos.Validation;

public static class EncerramentoValidador
{
    public static void Validar(DateTime dataInvestimento, DateTime dataEncerramento, DateTime hoje)
    {
        if (dataEncerramento.Date > hoje.Date)
            throw new CommandValidationException("A data de encerramento não pode ser futura.");

        if (dataEncerramento.Date < dataInvestimento.Date)
            throw new CommandValidationException("A data de encerramento não pode ser anterior à data do investimento.");
    }
}
