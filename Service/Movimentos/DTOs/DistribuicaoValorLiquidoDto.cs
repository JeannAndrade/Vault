using Application.Movimentos.DistribuicaoValorLiquido;

namespace Service.Movimentos.DTOs;

public record DistribuicaoValorLiquidoSolicitacaoDto
{
    public Guid TipoRendaId { get; init; }
    public Guid CorretoraId { get; init; }
    public Guid ProdutoId { get; init; }
    public Guid EmissorId { get; init; }
    public decimal ValorLiquidoTotal { get; init; }
    public DistribuicaoValorLiquidoParameters ToParameters() => new()
    {
        TipoRendaId = TipoRendaId, CorretoraId = CorretoraId, ProdutoId = ProdutoId,
        EmissorId = EmissorId, ValorLiquidoTotal = ValorLiquidoTotal
    };
}

public record AporteDistribuidoDto(Guid Id, string? ObjetivoNome, DateTime DataInvestimento,
    decimal ValorAporte, decimal ValorLiquidoAtual, decimal NovoValorLiquido, decimal Ganho);

public record DistribuicaoValorLiquidoDto(
    List<AporteDistribuidoDto> Aportes, decimal TotalAportado, decimal GanhoTotal,
    decimal ValorLiquidoTotal, double? TaxaEfetivaAnual, string? Aviso, bool PodeConfirmar)
{
    public static DistribuicaoValorLiquidoDto FromApplication(DistribuicaoValorLiquidoModel model) => new(
        [.. model.Aportes.Select(a => new AporteDistribuidoDto(a.Id, a.ObjetivoNome, a.DataInvestimento,
            a.ValorAporte, a.ValorLiquidoAtual, a.NovoValorLiquido, a.Ganho))],
        model.TotalAportado, model.GanhoTotal, model.ValorLiquidoTotal,
        model.TaxaEfetivaAnual, model.Aviso, model.PodeConfirmar);
}
