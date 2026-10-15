using Domain.Movimentos.Distribuicao;

namespace Domain.Test.Movimentos.Distribuicao;

public class DistribuicaoValorLiquidoCalculadorTests
{
    private static readonly DateTime DataReferencia = new(2026, 10, 14);

    [Fact]
    public void DistribuiPorCapitalETempoEFechaOTotal()
    {
        var aportes = new[]
        {
            Aporte(1200, -730), Aporte(2000, -365), Aporte(800, 0)
        };

        var resultado = Calcular(aportes, 5000);

        Assert.True(resultado.Sucesso);
        Assert.Equal(5000, resultado.Aportes!.Sum(aporte => aporte.ValorLiquido));
        Assert.Equal(1000, resultado.Aportes!.Sum(aporte => aporte.Ganho));
        Assert.True(resultado.Aportes![0].Ganho > resultado.Aportes[1].Ganho);
        Assert.InRange(resultado.Aportes[2].Ganho, -0.02m, 0.02m);
    }

    [Fact]
    public void CompensaCentavosNoAporteMaisRecente()
    {
        var resultado = Calcular(new[] { Aporte(1000, -365), Aporte(1000, -365) }, 3000.01m);
        Assert.Contains(resultado.Aportes!, aporte => aporte.Ganho == 500.01m);
        Assert.Contains(resultado.Aportes!, aporte => aporte.Ganho == 500m);
    }

    [Fact]
    public void TrataCasosDegenerados()
    {
        var aportes = new[] { Aporte(100, -365), Aporte(200, 0) };
        var semGanho = Calcular(aportes, 300);
        var perdaTotal = Calcular(aportes, 0);

        Assert.All(semGanho.Aportes!, aporte => Assert.Equal(0, aporte.Ganho));
        Assert.All(perdaTotal.Aportes!, aporte => Assert.Equal(0, aporte.ValorLiquido));
        Assert.Equal(-300, perdaTotal.Aportes!.Sum(aporte => aporte.Ganho));
    }

    [Fact]
    public void RejeitaSemBaseEValorNegativo()
    {
        var semBase = Calcular(new[] { Aporte(100, 0) }, 200);
        var extremo = Calcular(new[] { Aporte(100, -1095), Aporte(900, -4) }, 50);

        Assert.Equal(ErroDistribuicao.SemBaseDeTempo, semBase.Erro);
        Assert.Equal(ErroDistribuicao.ValorNegativo, extremo.Erro);
    }

    [Fact]
    public void ValidaPrecondicoes()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Calcular(Array.Empty<AporteParaDistribuicao>(), -1));
        Assert.Throws<ArgumentException>(() => Calcular(new[] { Aporte(0, 0) }, 1));
        Assert.Equal(ErroDistribuicao.SemAportes, Calcular(Array.Empty<AporteParaDistribuicao>(), 0).Erro);
    }

    private static AporteParaDistribuicao Aporte(decimal valor, int dias) =>
        new(Guid.NewGuid(), valor, DataReferencia.AddDays(dias));

    private static ResultadoDistribuicao Calcular(IReadOnlyList<AporteParaDistribuicao> aportes, decimal valor) =>
        DistribuicaoValorLiquidoCalculador.Calcular(aportes, valor, DataReferencia);
}
