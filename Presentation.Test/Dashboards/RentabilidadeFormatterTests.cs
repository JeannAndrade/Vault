using Presentation.Dashboards;

namespace Presentation.Test.Dashboards;

public class RentabilidadeFormatterTests
{
    [Theory]
    [InlineData(0.225, "22,50%")]
    [InlineData(-0.10, "-10,00%")]
    [InlineData(-1, "-100,00%")]
    [InlineData(0, "0,00%")] 
    [InlineData(-0.00001, "0,00%")] 
    [InlineData(12.345, "1.234,50%")] 
    public void Percentual_FormataEmPtBr(double fracao, string esperado) =>
        Assert.Equal(esperado, RentabilidadeFormatter.Percentual(fracao));

    [Fact]
    public void Percentual_FormataDiariaComQuatroCasas() =>
        Assert.Equal("0,0261%", RentabilidadeFormatter.Percentual(0.000261157876, 4));

    [Theory]
    [InlineData(0.1, "text-success")]
    [InlineData(-0.1, "text-danger")]
    [InlineData(0, "")]
    [InlineData(-0.00001, "")]
    public void ClasseDaRentabilidade_ConsideraValorArredondado(double fracao, string esperada) =>
        Assert.Equal(esperada, RentabilidadeFormatter.ClasseDaRentabilidade(fracao));
}
