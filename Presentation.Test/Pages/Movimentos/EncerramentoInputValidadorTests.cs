using Presentation.Movimentos;
using Presentation.Pages.Movimentos;

namespace Presentation.Test.Pages.Movimentos;

public class EncerramentoInputValidadorTests
{
    private static readonly DateTime Hoje = new(2026, 10, 27);

    [Fact]
    public void Validar_ObrigatorioSemMotivoEData_DevolveDoisErros()
    {
        var erros = EncerramentoInputValidador.Validar(new EncerramentoInput(), Hoje, obrigatorio: true);

        Assert.Equal(
            [
                (nameof(EncerramentoInput.Motivo), "Selecione o motivo do encerramento."),
                (nameof(EncerramentoInput.Data), "Informe a data do encerramento.")
            ],
            erros);
    }

    [Fact]
    public void Validar_ObrigatorioSemMotivo_DevolveErroNoMotivo()
    {
        var erros = EncerramentoInputValidador.Validar(
            new EncerramentoInput { Data = Hoje }, Hoje, obrigatorio: true);

        Assert.Equal((nameof(EncerramentoInput.Motivo), "Selecione o motivo do encerramento."), Assert.Single(erros));
    }

    [Fact]
    public void Validar_ObrigatorioSemData_DevolveErroNaData()
    {
        var erros = EncerramentoInputValidador.Validar(
            new EncerramentoInput { Motivo = MotivoEncerramento.Gasto }, Hoje, obrigatorio: true);

        Assert.Equal((nameof(EncerramentoInput.Data), "Informe a data do encerramento."), Assert.Single(erros));
    }

    [Fact]
    public void Validar_OpcionalSemCampos_NaoDevolveErros()
    {
        Assert.Empty(EncerramentoInputValidador.Validar(new EncerramentoInput(), Hoje, obrigatorio: false));
    }

    [Fact]
    public void Validar_OpcionalComSomenteMotivo_DevolveErroNaData()
    {
        var erros = EncerramentoInputValidador.Validar(
            new EncerramentoInput { Motivo = MotivoEncerramento.Gasto }, Hoje, obrigatorio: false);

        Assert.Equal((nameof(EncerramentoInput.Data), "Informe a data do encerramento."), Assert.Single(erros));
    }

    [Fact]
    public void Validar_OpcionalComSomenteData_DevolveErroNoMotivo()
    {
        var erros = EncerramentoInputValidador.Validar(
            new EncerramentoInput { Data = Hoje }, Hoje, obrigatorio: false);

        Assert.Equal((nameof(EncerramentoInput.Motivo), "Selecione o motivo do encerramento."), Assert.Single(erros));
    }

    [Fact]
    public void Validar_DataFutura_DevolveErroNaData()
    {
        var erros = EncerramentoInputValidador.Validar(
            new EncerramentoInput { Motivo = MotivoEncerramento.Gasto, Data = Hoje.AddDays(1) },
            Hoje,
            obrigatorio: false);

        Assert.Equal((nameof(EncerramentoInput.Data), "A data do encerramento não pode ser futura."), Assert.Single(erros));
    }

    [Fact]
    public void Validar_DataIgualHojeComHora_Passa()
    {
        var erros = EncerramentoInputValidador.Validar(
            new EncerramentoInput
            {
                Motivo = MotivoEncerramento.Gasto,
                Data = Hoje.AddHours(23).AddMinutes(59).AddSeconds(59)
            },
            Hoje,
            obrigatorio: true);

        Assert.Empty(erros);
    }
}
