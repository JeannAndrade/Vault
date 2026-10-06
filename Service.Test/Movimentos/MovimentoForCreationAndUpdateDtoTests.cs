using Service.Movimentos.DTOs;

namespace Service.Test.Movimentos;

public class MovimentoForCreationAndUpdateDtoTests
{
    [Fact]
    public void ToCreateMovimentoCommand_MapsDataVencimento()
    {
        var vencimento = new DateTime(2030, 1, 15);
        var dto = new MovimentoForCreationDto { DataVencimento = vencimento };

        var command = dto.ToCreateMovimentoCommand();

        Assert.Equal(vencimento, command.DataVencimento);
    }

    [Fact]
    public void ToUpdateMovimentoCommand_MapsDataVencimento()
    {
        var vencimento = new DateTime(2030, 1, 15);
        var dto = new MovimentoForUpdateDto { DataVencimento = vencimento };

        var command = dto.ToUpdateMovimentoCommand();

        Assert.Equal(vencimento, command.DataVencimento);
    }
}
