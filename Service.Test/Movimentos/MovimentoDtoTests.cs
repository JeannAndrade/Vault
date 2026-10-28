using Application.Movimentos;
using Application.Movimentos.Queries.GetMovimentosDesatualizados;
using Domain.Movimentos;
using LumiaFoundation.Core.Pagination;
using Service.Movimentos.DTOs;

namespace Service.Test.Movimentos;

public class MovimentoDtoTests
{
    [Fact]
    public void FromApplication_MapsDataVencimento()
    {
        var vencimento = new DateTime(2030, 1, 15);
        var model = new MovimentoModel { Id = Guid.NewGuid(), DataVencimento = vencimento };

        var dto = MovimentoDto.FromApplication(model);

        Assert.Equal(vencimento, dto.DataVencimento);
    }

    [Fact]
    public void FromApplication_WhenDataVencimentoIsNull_MapsAsNull()
    {
        // Ações, por exemplo, não têm vencimento.
        var model = new MovimentoModel { Id = Guid.NewGuid(), DataVencimento = null };

        var dto = MovimentoDto.FromApplication(model);

        Assert.Null(dto.DataVencimento);
    }

    [Fact]
    public void FromApplication_MapsEncerramentoAndKeepsStatus()
    {
        var model = new MovimentoModel
        {
            EstaAtivo = false,
            MotivoEncerramento = MotivoEncerramento.Gasto,
            DataEncerramento = new DateTime(2026, 10, 20)
        };

        var dto = MovimentoDto.FromApplication(model);

        Assert.False(dto.EstaAtivo);
        Assert.Equal(MotivoEncerramento.Gasto, dto.MotivoEncerramento);
        Assert.Equal(new DateTime(2026, 10, 20), dto.DataEncerramento);
    }

    [Fact]
    public void FromApplication_AtivoMapsEncerramentoComoNulo()
    {
        var model = new MovimentoModel { EstaAtivo = true };

        var dto = MovimentoDto.FromApplication(model);

        Assert.True(dto.EstaAtivo);
        Assert.Null(dto.MotivoEncerramento);
        Assert.Null(dto.DataEncerramento);
    }

    [Fact]
    public void MovimentoDesatualizadoFromApplication_MapsItemAndList()
    {
        var model = new MovimentoDesatualizadoModel
        {
            Id = Guid.NewGuid(),
            ObjetivoNome = "Aposentadoria",
            ValorLiquidoAtual = 123.45m
        };

        var item = MovimentoDesatualizadoDto.FromApplication(model);
        var lista = MovimentoDesatualizadoDto.FromApplication([model]);

        Assert.Equal(model.Id, item.Id);
        Assert.Equal(model.ObjetivoNome, item.ObjetivoNome);
        Assert.Equal(model.ValorLiquidoAtual, item.ValorLiquidoAtual);
        Assert.Equal(item, Assert.Single(lista));
    }
}
