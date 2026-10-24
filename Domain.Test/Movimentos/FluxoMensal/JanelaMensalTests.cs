using Domain.Movimentos.FluxoMensal;

namespace Domain.Test.Movimentos.FluxoMensal;

public class JanelaMensalTests
{
    [Theory]
    [InlineData(2026, 10, 2026, 10, 2027, 9, 2025, 11, 2026, 10)]
    [InlineData(2026, 12, 2026, 12, 2027, 11, 2026, 1, 2026, 12)]
    [InlineData(2026, 1, 2026, 1, 2026, 12, 2025, 2, 2026, 1)]
    [InlineData(2028, 2, 2028, 2, 2029, 1, 2027, 3, 2028, 2)]
    public void GeraDozeMesesContiguos(int ano, int mes, int proxAno, int proxMes, int proxUltimoAno, int proxUltimoMes, int antAno, int antMes, int antUltimoAno, int antUltimoMes)
    {
        var hoje = new DateTime(ano, mes, 24, 23, 59, 59, DateTimeKind.Utc);
        Verificar(JanelaMensal.Proximos(hoje), proxAno, proxMes, proxUltimoAno, proxUltimoMes);
        Verificar(JanelaMensal.Anteriores(hoje), antAno, antMes, antUltimoAno, antUltimoMes);
    }

    [Fact]
    public void IgnoraHoraEKind() =>
        Assert.Equal(JanelaMensal.Proximos(new DateTime(2026, 10, 24)), JanelaMensal.Proximos(new DateTime(2026, 10, 24, 23, 59, 59, DateTimeKind.Local)));

    [Fact]
    public void QuantidadeMesesValeDoze() => Assert.Equal(12, JanelaMensal.QuantidadeMeses);

    private static void Verificar(IReadOnlyList<IntervaloMensal> meses, int primeiroAno, int primeiroMes, int ultimoAno, int ultimoMes)
    {
        Assert.Equal(12, meses.Count);
        Assert.Equal((primeiroAno, primeiroMes), (meses[0].Ano, meses[0].Mes));
        Assert.Equal((ultimoAno, ultimoMes), (meses[^1].Ano, meses[^1].Mes));
        Assert.Equal(1, meses[0].Inicio.Day);
        Assert.Equal(0, meses[0].Inicio.Hour);
        for (var i = 0; i < meses.Count - 1; i++) Assert.Equal(meses[i].Fim, meses[i + 1].Inicio);
        Assert.All(meses, mes => Assert.Equal(DateTimeKind.Unspecified, mes.Inicio.Kind));
    }
}
