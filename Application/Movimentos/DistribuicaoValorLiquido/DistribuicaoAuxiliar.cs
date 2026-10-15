using Domain.Movimentos;
using Domain.Movimentos.Distribuicao;

namespace Application.Movimentos.DistribuicaoValorLiquido;

internal static class DistribuicaoAuxiliar
{
    internal static Dictionary<Guid, Movimento> PorId(IEnumerable<Movimento> movimentos) => movimentos.ToDictionary(m => m.Id);
    internal static List<AporteParaDistribuicao> Aportes(IEnumerable<Movimento> movimentos) => [.. movimentos.Select(m => new AporteParaDistribuicao(m.Id, m.ValorAporte, m.DataInvestimento))];
}
