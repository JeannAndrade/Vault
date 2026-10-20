using Domain.Movimentos.Rentabilidade;

namespace Domain.Test.Movimentos.Rentabilidade;

public class RentabilidadeCalculadorTests
{
    private static readonly DateTime DataBase = new(2026, 1, 1);

    [Theory]
    [InlineData(1000, 1100, 365, 0.1, 0.000261157876, 0.1)]
    [InlineData(1000, 1050, 100, 0.05, 0.000488020685, 0.194925809336)]
    [InlineData(1000, 900, 365, -0.1, -0.000288617289, -0.1)]
    [InlineData(1000, 1210, 730, 0.21, 0.000261157876, 0.1)]
    public void CalculaRentabilidades(decimal aporte, decimal valor, int dias, double acumulada, double diaria, double anualizada)
    {
        var resultado = Calcular(aporte, valor, dias);

        Assert.NotNull(resultado);
        Assert.Equal(acumulada, resultado.Acumulada, precision: 12);
        Assert.Equal(diaria, resultado.Diaria, precision: 12);
        Assert.Equal(anualizada, resultado.Anualizada, precision: 12);
        Assert.Equal(dias, resultado.Dias);
    }

    [Fact]
    public void TrataVariacaoZeroEPerdaTotal()
    {
        var semVariacao = Calcular(1000, 1000, 200);
        var perdaTotal = Calcular(1000, 0, 100);

        Assert.NotNull(semVariacao);
        Assert.Equal(0, semVariacao.Acumulada);
        Assert.Equal(0, semVariacao.Diaria);
        Assert.Equal(0, semVariacao.Anualizada);
        Assert.NotNull(perdaTotal);
        Assert.Equal(-1, perdaTotal.Acumulada);
        Assert.Equal(-1, perdaTotal.Diaria);
        Assert.Equal(-1, perdaTotal.Anualizada);
    }

    [Fact]
    public void RejeitaDatasValoresInvalidosEOverflow()
    {
        Assert.Null(Calcular(1000, 1100, 0));
        Assert.Null(Calcular(1000, 1100, -1));
        Assert.Null(Calcular(0, 1100, 100));
        Assert.Null(Calcular(-1, 1100, 100));
        Assert.Null(Calcular(1000, -1, 100));
        Assert.Null(Calcular(1, 1e28m, 1));
    }

    [Fact]
    public void UsaSomenteADataEElegibilidadeTemLimiteDeTrintaDias()
    {
        var resultado = RentabilidadeCalculador.Calcular(
            1000,
            1100,
            DataBase.AddHours(23).AddMinutes(59),
            DataBase.AddDays(30).AddMinutes(1));

        Assert.NotNull(resultado);
        Assert.Equal(30, resultado.Dias);
        Assert.True(resultado.ElegivelParaRanking);
        Assert.False(Calcular(1000, 1100, 29)!.ElegivelParaRanking);
        Assert.Equal(30, RentabilidadeCalculador.DiasMinimosParaRanking);
    }

    private static ResultadoRentabilidade? Calcular(decimal aporte, decimal valor, int dias) =>
        RentabilidadeCalculador.Calcular(aporte, valor, DataBase, DataBase.AddDays(dias));
}
