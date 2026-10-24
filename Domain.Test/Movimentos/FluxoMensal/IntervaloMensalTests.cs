using Domain.Movimentos.FluxoMensal;

namespace Domain.Test.Movimentos.FluxoMensal;

public class IntervaloMensalTests
{
    private static readonly IntervaloMensal Intervalo = new(2026, 1, new DateTime(2026, 1, 1), new DateTime(2026, 2, 1));

    [Fact]
    public void ContemRespeitaIntervaloSemiaberto()
    {
        Assert.True(Intervalo.Contem(Intervalo.Inicio));
        Assert.False(Intervalo.Contem(Intervalo.Fim));
        Assert.True(Intervalo.Contem(Intervalo.Fim.AddTicks(-1)));
        Assert.False(Intervalo.Contem(Intervalo.Inicio.AddTicks(-1)));
        Assert.True(Intervalo.Contem(new DateTime(2026, 1, 15)));
    }
}
