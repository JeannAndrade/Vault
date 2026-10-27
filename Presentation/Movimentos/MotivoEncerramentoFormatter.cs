namespace Presentation.Movimentos;

public static class MotivoEncerramentoFormatter
{
    public static string Descricao(MotivoEncerramento? motivo) => motivo switch
    {
        MotivoEncerramento.Gasto => "Gasto",
        MotivoEncerramento.Reinvestido => "Reinvestido",
        MotivoEncerramento.Pendente => "Pendente (sem destino definido)",
        null => string.Empty,
        _ => throw new ArgumentOutOfRangeException(nameof(motivo), motivo, "Motivo de encerramento inválido.")
    };
}
