using Domain.Movimentos;
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
    public void ToCreateMovimentoCommand_MapsEncerramentoOpcional()
    {
        var dataEncerramento = new DateTime(2026, 10, 20);
        var dto = new MovimentoForCreationDto
        {
            MotivoEncerramento = MotivoEncerramento.Reinvestido,
            DataEncerramento = dataEncerramento
        };

        var command = dto.ToCreateMovimentoCommand();

        Assert.Equal(MotivoEncerramento.Reinvestido, command.MotivoEncerramento);
        Assert.Equal(dataEncerramento, command.DataEncerramento);
    }

    [Fact]
    public void ToCreateMovimentoCommand_SemEncerramentoMantemCamposNulos()
    {
        var command = new MovimentoForCreationDto().ToCreateMovimentoCommand();

        Assert.Null(command.MotivoEncerramento);
        Assert.Null(command.DataEncerramento);
    }

    [Theory]
    [InlineData(typeof(MovimentoForCreationDto))]
    [InlineData(typeof(MovimentoForUpdateDto))]
    public void DtosDeCriacaoEEdicaoNaoExpoemEstaAtivo(Type tipoDto)
    {
        Assert.Null(tipoDto.GetProperty("EstaAtivo"));
    }

    [Fact]
    public void ToUpdateMovimentoCommand_MapsDataVencimento()
    {
        var vencimento = new DateTime(2030, 1, 15);
        var dto = new MovimentoForUpdateDto { DataVencimento = vencimento };

        var command = dto.ToUpdateMovimentoCommand();

        Assert.Equal(vencimento, command.DataVencimento);
    }

    [Fact]
    public void ToUpdateValorLiquidoCommand_MapsValorLiquidoAtual()
    {
        var dto = new MovimentoForUpdateValorLiquidoDto { ValorLiquidoAtual = 1234.56m };

        var command = dto.ToUpdateValorLiquidoCommand();

        Assert.Equal(dto.ValorLiquidoAtual, command.ValorLiquidoAtual);
    }
}
