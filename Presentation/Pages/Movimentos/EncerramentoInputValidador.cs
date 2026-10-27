namespace Presentation.Pages.Movimentos;

public static class EncerramentoInputValidador
{
    public static IReadOnlyList<(string Campo, string Mensagem)> Validar(
        EncerramentoInput input,
        DateTime hoje,
        bool obrigatorio)
    {
        var erros = new List<(string Campo, string Mensagem)>();

        if (input.Motivo is null && (obrigatorio || input.Data.HasValue))
            erros.Add((nameof(input.Motivo), "Selecione o motivo do encerramento."));

        if (input.Data is null && (obrigatorio || input.Motivo.HasValue))
            erros.Add((nameof(input.Data), "Informe a data do encerramento."));
        else if (input.Data?.Date > hoje.Date)
            erros.Add((nameof(input.Data), "A data do encerramento não pode ser futura."));

        return erros;
    }
}
