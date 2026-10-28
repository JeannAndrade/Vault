using System.Globalization;
using Presentation.Objetivos;

namespace Presentation.Test.Objetivos;

public class ProjecaoFormatterTests
{
    [Theory]
    [InlineData(1, "1 mês (mar/2028)")]
    [InlineData(18, "18 meses (mar/2028)")]
    public void FormatarCenario_Prevista(int meses, string esperado) =>
        Assert.Equal(esperado, ProjecaoFormatter.FormatarCenario(new(SituacaoProjecao.Prevista, meses, new DateTime(2028, 3, 1))));

    [Theory]
    [InlineData(SituacaoProjecao.Atingida, "Meta atingida")]
    [InlineData(SituacaoProjecao.MaisDeCemAnos, "Mais de 100 anos")]
    [InlineData(SituacaoProjecao.SemPrevisao, "Sem previsão")]
    public void FormatarCenario_Situacoes(SituacaoProjecao situacao, string esperado) =>
        Assert.Equal(esperado, ProjecaoFormatter.FormatarCenario(new(situacao, null, null)));

    [Fact]
    public void FormatarCenario_DadosIncompletosESemCenario_NaoLanca()
    {
        Assert.Equal("Sem previsão", ProjecaoFormatter.FormatarCenario(null));
        Assert.Equal("Sem previsão", ProjecaoFormatter.FormatarCenario(new(SituacaoProjecao.Prevista, null, DateTime.Today)));
        Assert.Equal("Sem previsão", ProjecaoFormatter.FormatarCenario(new(SituacaoProjecao.Prevista, 1, null)));
    }

    [Fact]
    public void FormatarMesAno_UsaTabelaFixaDeMeses()
    {
        var culturaAnterior = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            Assert.Equal("mar/2028", ProjecaoFormatter.FormatarMesAno(new DateTime(2028, 3, 1)));
        }
        finally { CultureInfo.CurrentCulture = culturaAnterior; }
    }

    [Theory]
    [InlineData(0, "0,00%")]
    [InlineData(.3, "30,00%")]
    [InlineData(.12345, "12,35%")]
    public void FormatarTaxa(double taxa, string esperado) => Assert.Equal(esperado, ProjecaoFormatter.FormatarTaxa(taxa));

    [Fact]
    public void FormatarTaxa_ValorNaoFinitoRetornaTraco() => Assert.Equal("—", ProjecaoFormatter.FormatarTaxa(double.NaN));

    [Fact]
    public void FormatarAporteRealizado_FormataHistoricoESingular()
    {
        var cultura = CultureInfo.GetCultureInfo("pt-BR");
        Assert.Equal("Sem histórico de aportes", ProjecaoFormatter.FormatarAporteRealizado(null, 0));
        Assert.Equal($"{500m.ToString("C", cultura)} por mês (média de 8 meses)", ProjecaoFormatter.FormatarAporteRealizado(500m, 8));
        Assert.Equal($"{500m.ToString("C", cultura)} por mês (média de 1 mês)", ProjecaoFormatter.FormatarAporteRealizado(500m, 1));
    }
}
