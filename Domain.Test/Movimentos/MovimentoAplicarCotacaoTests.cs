using Domain.Movimentos;

namespace Domain.Test.Movimentos;

public class MovimentoAplicarCotacaoTests
{
    [Theory]
    [InlineData(20, 40, 800)]
    [InlineData(40, 40, 1600)]
    public void CalculaQuantidadeVezesCotacao(decimal quantidade, decimal cotacao, decimal esperado)
    {
        var movimento = new Movimento { Quantidade = quantidade };

        movimento.AplicarCotacao(cotacao, new DateTime(2026, 10, 18, 13, 45, 00));

        Assert.Equal(esperado, movimento.ValorLiquidoAtual);
    }

    [Fact]
    public void ArredondaParaDuasCasasComAwayFromZero()
    {
        var movimento = new Movimento { Quantidade = 1.005m };

        movimento.AplicarCotacao(1m, DateTime.UtcNow);

        Assert.Equal(1.01m, movimento.ValorLiquidoAtual);
    }

    [Fact]
    public void GravaSomenteADataDeAtualizacao()
    {
        var movimento = new Movimento { Quantidade = 2m };
        var hoje = new DateTime(2026, 10, 18, 23, 59, 59);

        movimento.AplicarCotacao(10m, hoje);

        Assert.Equal(hoje.Date, movimento.DataAtualizacaoValorLiquido);
    }

    [Fact]
    public void RejeitaQuantidadeNula()
    {
        var movimento = new Movimento { Quantidade = null };

        Assert.Throws<InvalidOperationException>(() => movimento.AplicarCotacao(10m, DateTime.UtcNow));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RejeitaQuantidadeNaoPositiva(int quantidade)
    {
        var movimento = new Movimento { Quantidade = quantidade };

        Assert.Throws<InvalidOperationException>(() => movimento.AplicarCotacao(10m, DateTime.UtcNow));
    }

    [Fact]
    public void PreservaOsDemaisCampos()
    {
        var movimento = new Movimento
        {
            Quantidade = 3m,
            ValorAporte = 123.45m,
            Observacao = "Observação original"
        };

        movimento.AplicarCotacao(10m, DateTime.UtcNow);

        Assert.Equal(123.45m, movimento.ValorAporte);
        Assert.Equal("Observação original", movimento.Observacao);
    }
}
