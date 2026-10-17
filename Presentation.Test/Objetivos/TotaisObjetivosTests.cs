using Presentation.Objetivos;

namespace Presentation.Test.Objetivos;

public class TotaisObjetivosTests
{
    [Fact]
    public void FromResumo_VariosObjetivos_SomaOsTresCampos()
    {
        var objetivos = new[]
        {
            CriarResumo(2, 10.25m, 8.10m),
            CriarResumo(5, 20.75m, 19.90m)
        };

        var resultado = TotaisObjetivos.FromResumo(objetivos);

        Assert.Equal(7, resultado.QtdeMovimentos);
        Assert.Equal(31.00m, resultado.TotalAportado);
        Assert.Equal(28.00m, resultado.TotalLiquido);
    }

    [Fact]
    public void FromResumo_ObjetivoUnico_RetornaValoresDaLinha()
    {
        var objetivo = CriarResumo(3, 12.34m, 11.23m);

        var resultado = TotaisObjetivos.FromResumo([objetivo]);

        Assert.Equal(3, resultado.QtdeMovimentos);
        Assert.Equal(12.34m, resultado.TotalAportado);
        Assert.Equal(11.23m, resultado.TotalLiquido);
    }

    [Fact]
    public void FromResumo_ListaVazia_RetornaVazio()
    {
        var resultado = TotaisObjetivos.FromResumo([]);

        Assert.Equal(TotaisObjetivos.Vazio, resultado);
    }

    [Fact]
    public void FromResumo_PreservaPrecisaoDecimal()
    {
        var resultado = TotaisObjetivos.FromResumo(
        [
            CriarResumo(0, 0.10m, 0.10m),
            CriarResumo(0, 0.20m, 0.20m)
        ]);

        Assert.Equal(0.30m, resultado.TotalAportado);
        Assert.Equal(0.30m, resultado.TotalLiquido);
    }

    [Fact]
    public void FromResumo_MetaEPercentualNaoInfluenciam()
    {
        var primeiro = new ResumoObjetivoDto(Guid.NewGuid(), "Objetivo", 100m, 10m, 2, 10m, 8m);
        var segundo = primeiro with { Meta = 999m, PercentualMeta = 999m };

        Assert.Equal(TotaisObjetivos.FromResumo([primeiro]), TotaisObjetivos.FromResumo([segundo]));
    }

    [Fact]
    public void FromResumo_ListaNula_LancaArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => TotaisObjetivos.FromResumo(null!));
    }

    private static ResumoObjetivoDto CriarResumo(int movimentos, decimal aportado, decimal liquido) =>
        new(Guid.NewGuid(), "Objetivo", 0m, 0m, movimentos, aportado, liquido);
}
