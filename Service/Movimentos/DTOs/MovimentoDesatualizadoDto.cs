using Application.Movimentos.Queries.GetMovimentosDesatualizados;

namespace Service.Movimentos.DTOs;

public record MovimentoDesatualizadoDto
{
    public Guid Id { get; init; }
    public string? ObjetivoNome { get; init; }
    public string? CorretoraNome { get; init; }
    public string? ProdutoNome { get; init; }
    public string? EmissorNome { get; init; }
    public DateTime? DataVencimento { get; init; }
    public decimal ValorLiquidoAtual { get; init; }

    public static MovimentoDesatualizadoDto FromApplication(MovimentoDesatualizadoModel movimento) => new()
    {
        Id = movimento.Id,
        ObjetivoNome = movimento.ObjetivoNome,
        CorretoraNome = movimento.CorretoraNome,
        ProdutoNome = movimento.ProdutoNome,
        EmissorNome = movimento.EmissorNome,
        DataVencimento = movimento.DataVencimento,
        ValorLiquidoAtual = movimento.ValorLiquidoAtual
    };

    public static List<MovimentoDesatualizadoDto> FromApplication(List<MovimentoDesatualizadoModel> movimentos) =>
        [.. movimentos.Select(FromApplication)];
}
