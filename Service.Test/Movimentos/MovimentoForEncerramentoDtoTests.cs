using Domain.Movimentos;
using Service.Movimentos.DTOs;

namespace Service.Test.Movimentos;

public class MovimentoForEncerramentoDtoTests
{
    [Fact]
    public void ToEncerrarMovimentoCommand_MapeiaMotivoEData()
    {
        var dto = new MovimentoForEncerramentoDto
        {
            Motivo = MotivoEncerramento.Pendente,
            Data = new DateTime(2026, 10, 20)
        };

        var command = dto.ToEncerrarMovimentoCommand();

        Assert.Equal(MotivoEncerramento.Pendente, command.Motivo);
        Assert.Equal(new DateTime(2026, 10, 20), command.Data);
    }

    [Fact]
    public void ToEncerrarMovimentoCommand_PreservaCamposNulos()
    {
        var command = new MovimentoForEncerramentoDto().ToEncerrarMovimentoCommand();

        Assert.Null(command.Motivo);
        Assert.Null(command.Data);
    }
}
