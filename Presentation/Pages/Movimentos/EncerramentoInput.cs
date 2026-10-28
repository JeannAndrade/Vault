using Presentation.Movimentos;

namespace Presentation.Pages.Movimentos;

public sealed class EncerramentoInput
{
    public MotivoEncerramento? Motivo { get; set; }
    public DateTime? Data { get; set; }
}
