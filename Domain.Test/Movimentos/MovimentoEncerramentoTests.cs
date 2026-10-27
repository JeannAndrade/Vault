using Domain.Movimentos;

namespace Domain.Test.Movimentos;

public class MovimentoEncerramentoTests
{
    private static Movimento CriarMovimentoAtivo(DateTime? dataInvestimento = null)
    {
        return new Movimento
        {
            DataInvestimento = dataInvestimento ?? new DateTime(2026, 10, 01),
            EstaAtivo = true,
            Quantidade = 10m,
            ValorLiquidoAtual = 100m,
            DataAtualizacaoValorLiquido = new DateTime(2026, 10, 15),
            ValorAporte = 123.45m,
            Observacao = "Observação original",
            EhReinvestimento = true
        };
    }

    [Theory]
    [InlineData(MotivoEncerramento.Gasto)]
    [InlineData(MotivoEncerramento.Reinvestido)]
    [InlineData(MotivoEncerramento.Pendente)]
    public void EncerrarComCadaMotivo_GravaEstadoDeEncerramento(MotivoEncerramento motivo)
    {
        var movimento = CriarMovimentoAtivo();
        var hoje = new DateTime(2026, 10, 18);

        movimento.Encerrar(motivo, hoje.AddDays(-1), hoje);

        Assert.False(movimento.EstaAtivo);
        Assert.Equal(motivo, movimento.MotivoEncerramento);
        Assert.Equal(hoje.AddDays(-1).Date, movimento.DataEncerramento);
    }

    [Fact]
    public void EncerrarComDataComHora_GravaSomenteAData()
    {
        var movimento = CriarMovimentoAtivo();
        var data = new DateTime(2026, 10, 18, 23, 59, 59);

        movimento.Encerrar(MotivoEncerramento.Gasto, data, data);

        Assert.Equal(data.Date, movimento.DataEncerramento);
    }

    [Fact]
    public void EncerrarComDataIgualHoje_Aceita()
    {
        var movimento = CriarMovimentoAtivo();
        var hoje = new DateTime(2026, 10, 18);

        movimento.Encerrar(MotivoEncerramento.Gasto, hoje, hoje);

        Assert.False(movimento.EstaAtivo);
        Assert.Equal(MotivoEncerramento.Gasto, movimento.MotivoEncerramento);
    }

    [Fact]
    public void EncerrarComDataIgualADataInvestimento_Aceita()
    {
        var movimento = CriarMovimentoAtivo(new DateTime(2026, 10, 18));
        var hoje = new DateTime(2026, 10, 18);

        movimento.Encerrar(MotivoEncerramento.Gasto, movimento.DataInvestimento, hoje);

        Assert.False(movimento.EstaAtivo);
        Assert.Equal(movimento.DataInvestimento.Date, movimento.DataEncerramento);
    }

    [Fact]
    public void EncerrarComDataFutura_RolaExcecaoEPreservaEstado()
    {
        var movimento = CriarMovimentoAtivo();
        var hoje = new DateTime(2026, 10, 18);
        var dataFutura = hoje.AddDays(1);

        Assert.Throws<InvalidOperationException>(() => movimento.Encerrar(MotivoEncerramento.Gasto, dataFutura, hoje));

        Assert.True(movimento.EstaAtivo);
        Assert.Null(movimento.MotivoEncerramento);
        Assert.Null(movimento.DataEncerramento);
    }

    [Fact]
    public void EncerrarComDataAntesDaDataInvestimento_RolaExcecaoEPreservaEstado()
    {
        var movimento = CriarMovimentoAtivo(new DateTime(2026, 10, 10));
        var hoje = new DateTime(2026, 10, 18);
        var dataAntes = new DateTime(2026, 10, 09);

        Assert.Throws<InvalidOperationException>(() => movimento.Encerrar(MotivoEncerramento.Gasto, dataAntes, hoje));

        Assert.True(movimento.EstaAtivo);
        Assert.Null(movimento.MotivoEncerramento);
        Assert.Null(movimento.DataEncerramento);
    }

    [Fact]
    public void EncerrarComMotivoForaDoEnum_RolaExcecaoEPreservaEstado()
    {
        var movimento = CriarMovimentoAtivo();
        var hoje = new DateTime(2026, 10, 18);
        var motivoInvalido = (MotivoEncerramento)99;

        Assert.Throws<ArgumentOutOfRangeException>(() => movimento.Encerrar(motivoInvalido, hoje, hoje));

        Assert.True(movimento.EstaAtivo);
        Assert.Null(movimento.MotivoEncerramento);
        Assert.Null(movimento.DataEncerramento);
    }

    [Fact]
    public void ReclassificarMovimentoEncerrado_AtualizaMotivoEData()
    {
        var movimento = CriarMovimentoAtivo();
        var hoje = new DateTime(2026, 10, 18);

        movimento.Encerrar(MotivoEncerramento.Pendente, hoje.AddDays(-2), hoje);
        movimento.Encerrar(MotivoEncerramento.Gasto, hoje.AddDays(-1), hoje);

        Assert.False(movimento.EstaAtivo);
        Assert.Equal(MotivoEncerramento.Gasto, movimento.MotivoEncerramento);
        Assert.Equal(hoje.AddDays(-1).Date, movimento.DataEncerramento);
    }

    [Fact]
    public void ReclassificacaoComDataInvalida_PreservaEstadoAnterior()
    {
        var movimento = CriarMovimentoAtivo();
        var hoje = new DateTime(2026, 10, 18);

        movimento.Encerrar(MotivoEncerramento.Pendente, hoje.AddDays(-2), hoje);

        Assert.Throws<InvalidOperationException>(() => movimento.Encerrar(MotivoEncerramento.Gasto, hoje.AddDays(1), hoje));

        Assert.Equal(MotivoEncerramento.Pendente, movimento.MotivoEncerramento);
        Assert.Equal(hoje.AddDays(-2).Date, movimento.DataEncerramento);
    }

    [Fact]
    public void EncerrarEReativar_PreservamOsDemaisCampos()
    {
        var movimento = CriarMovimentoAtivo();
        var hoje = new DateTime(2026, 10, 18);

        movimento.Encerrar(MotivoEncerramento.Gasto, hoje.AddDays(-1), hoje);
        movimento.Reativar();

        Assert.True(movimento.EstaAtivo);
        Assert.Equal(123.45m, movimento.ValorAporte);
        Assert.Equal("Observação original", movimento.Observacao);
        Assert.Equal(100m, movimento.ValorLiquidoAtual);
        Assert.Equal(new DateTime(2026, 10, 15), movimento.DataAtualizacaoValorLiquido);
        Assert.True(movimento.EhReinvestimento);
        Assert.Null(movimento.MotivoEncerramento);
        Assert.Null(movimento.DataEncerramento);
    }

    [Fact]
    public void ReativarEmMovimentoJaAtivo_NaoFazNada()
    {
        var movimento = CriarMovimentoAtivo();

        movimento.Reativar();

        Assert.True(movimento.EstaAtivo);
        Assert.Null(movimento.MotivoEncerramento);
        Assert.Null(movimento.DataEncerramento);
    }

    [Fact]
    public void CicloEncerrarReativarEncerrar_AceitaNovoEncerramento()
    {
        var movimento = CriarMovimentoAtivo();
        var hoje = new DateTime(2026, 10, 18);

        movimento.Encerrar(MotivoEncerramento.Pendente, hoje.AddDays(-2), hoje);
        movimento.Reativar();
        movimento.Encerrar(MotivoEncerramento.Gasto, hoje.AddDays(-1), hoje);

        Assert.False(movimento.EstaAtivo);
        Assert.Equal(MotivoEncerramento.Gasto, movimento.MotivoEncerramento);
        Assert.Equal(hoje.AddDays(-1).Date, movimento.DataEncerramento);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void InvarianteEManterComSequenciasDeOperacoes(int sequencia)
    {
        var movimento = CriarMovimentoAtivo();
        var hoje = new DateTime(2026, 10, 18);

        if (sequencia == 0)
        {
            Assert.True(movimento.EstaAtivo == (movimento.MotivoEncerramento is null));
            Assert.True((movimento.MotivoEncerramento is null) == (movimento.DataEncerramento is null));
            return;
        }

        if (sequencia == 1)
        {
            movimento.Encerrar(MotivoEncerramento.Gasto, hoje.AddDays(-1), hoje);
        }
        else if (sequencia == 2)
        {
            movimento.Encerrar(MotivoEncerramento.Reinvestido, hoje.AddDays(-1), hoje);
            movimento.Reativar();
        }
        else
        {
            movimento.Encerrar(MotivoEncerramento.Pendente, hoje.AddDays(-2), hoje);
            movimento.Encerrar(MotivoEncerramento.Gasto, hoje.AddDays(-1), hoje);
            movimento.Reativar();
            movimento.Encerrar(MotivoEncerramento.Gasto, hoje.AddDays(-3), hoje);
        }

        Assert.True(movimento.EstaAtivo == (movimento.MotivoEncerramento is null));
        Assert.True((movimento.MotivoEncerramento is null) == (movimento.DataEncerramento is null));
    }
}
