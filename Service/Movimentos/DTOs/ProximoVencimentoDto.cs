using Application.Movimentos.Queries.GetProximosVencimentos;

namespace Service.Movimentos.DTOs;

public class ProximoVencimentoDto
{
    public Guid Id { get; set; }
    public string? ObjetivoNome { get; set; }
    public string? CorretoraNome { get; set; }
    public string? ProdutoNome { get; set; }
    public string? EmissorNome { get; set; }
    public DateTime? DataVencimento { get; set; }
    public decimal ValorLiquidoAtual { get; set; }

    public static ProximoVencimentoDto FromApplication(ProximoVencimentoModel movimento) => new()
    {
        Id = movimento.Id,
        ObjetivoNome = movimento.ObjetivoNome,
        CorretoraNome = movimento.CorretoraNome,
        ProdutoNome = movimento.ProdutoNome,
        EmissorNome = movimento.EmissorNome,
        DataVencimento = movimento.DataVencimento,
        ValorLiquidoAtual = movimento.ValorLiquidoAtual
    };

    public static List<ProximoVencimentoDto> FromApplication(List<ProximoVencimentoModel> movimentos) =>
        [.. movimentos.Select(FromApplication)];
}
