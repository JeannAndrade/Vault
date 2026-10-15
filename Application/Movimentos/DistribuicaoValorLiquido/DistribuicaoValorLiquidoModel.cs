using Domain.Movimentos.Distribuicao;

namespace Application.Movimentos.DistribuicaoValorLiquido;

public record AporteDistribuidoModel(Guid Id, string? ObjetivoNome, DateTime DataInvestimento, decimal ValorAporte, decimal ValorLiquidoAtual, decimal NovoValorLiquido, decimal Ganho);
public record DistribuicaoValorLiquidoModel(List<AporteDistribuidoModel> Aportes, decimal TotalAportado, decimal GanhoTotal, decimal ValorLiquidoTotal, double? TaxaEfetivaAnual, string? Aviso)
{
    public bool PodeConfirmar => Aviso is null && Aportes.Count > 0;
    internal static string MensagemDoErro(ErroDistribuicao erro) => erro switch
    {
        ErroDistribuicao.SemAportes => "Não há aportes ativos para o título selecionado.",
        ErroDistribuicao.SemBaseDeTempo => "Todos os aportes do título foram feitos hoje; não há tempo de aplicação para distribuir o rendimento.",
        ErroDistribuicao.ValorNegativo => "A distribuição resultaria em valor negativo para algum aporte. Atualize os movimentos individualmente.",
        _ => "Não foi possível distribuir o valor informado para este título."
    };
}
