using Presentation.Dashboards;

namespace Presentation.Test.Dashboards;

public class ConsolidadoFormatterTests
{
    [Theory]
    [InlineData(DimensaoConsolidado.TipoRenda, "Tipo de Renda")]
    [InlineData(DimensaoConsolidado.Corretora, "Corretora")]
    [InlineData(DimensaoConsolidado.Produto, "Produto")]
    [InlineData(DimensaoConsolidado.Emissor, "Emissor")]
    public void RotulaDimensao(DimensaoConsolidado dimensao, string esperado) => Assert.Equal(esperado, ConsolidadoFormatter.RotuloDimensao(dimensao));

    [Fact]
    public void RotulaDimensaoNulaEPercentuais() {
        Assert.Equal("", ConsolidadoFormatter.RotuloDimensao(null));
        Assert.Equal("60,00%", ConsolidadoFormatter.Percentual(0.6m));
        Assert.Equal("100,00%", ConsolidadoFormatter.Percentual(1m));
        Assert.Equal("0,00%", ConsolidadoFormatter.Percentual(0m));
        Assert.Equal("36,71%", ConsolidadoFormatter.Percentual(0.36709m));
    }

    [Fact]
    public void NomeDoGrupoUsaTracoParaAusentes() {
        Assert.Equal("Inter", ConsolidadoFormatter.NomeDoGrupo("Inter"));
        Assert.Equal("—", ConsolidadoFormatter.NomeDoGrupo(null));
        Assert.Equal("—", ConsolidadoFormatter.NomeDoGrupo(""));
    }
}
