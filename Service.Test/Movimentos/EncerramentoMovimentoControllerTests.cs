using Application.Movimentos.Commands.EncerrarMovimento;
using Application.Movimentos.Commands.ReativarMovimento;
using Domain.Movimentos;
using LumiaFoundation.AspNetCore.ActionFilters;
using LumiaFoundation.Core.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Service.Movimentos;
using Service.Movimentos.DTOs;

namespace Service.Test.Movimentos;

public class EncerramentoMovimentoControllerTests
{
    private readonly Mock<IEncerrarMovimentoCommand> _encerrar = new();
    private readonly Mock<IReativarMovimentoCommand> _reativar = new();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly EncerramentoMovimentoController _controller;

    public EncerramentoMovimentoControllerTests()
    {
        _controller = new EncerramentoMovimentoController(_encerrar.Object, _reativar.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        _controller.HttpContext.Items["UserId"] = _userId.ToString();
    }

    [Fact]
    public async Task Encerrar_RepassaModeloUsuarioIdETokenERetornaNoContent()
    {
        using var cts = new CancellationTokenSource();
        var id = Guid.NewGuid();
        var data = new DateTime(2026, 10, 20);
        var dto = new MovimentoForEncerramentoDto { Motivo = MotivoEncerramento.Gasto, Data = data };

        var result = await _controller.Encerrar(id, dto, cts.Token);

        Assert.IsType<NoContentResult>(result);
        _encerrar.Verify(c => c.ExecuteAsync(
            It.Is<MovimentoModelForEncerramento>(m => m.Motivo == dto.Motivo && m.Data == dto.Data),
            _userId,
            id,
            cts.Token), Times.Once);
    }

    [Fact]
    public async Task Encerrar_ComCamposNulos_RepassaNulosAoComando()
    {
        var id = Guid.NewGuid();

        await _controller.Encerrar(id, new MovimentoForEncerramentoDto(), CancellationToken.None);

        _encerrar.Verify(c => c.ExecuteAsync(
            It.Is<MovimentoModelForEncerramento>(m => m.Motivo == null && m.Data == null),
            _userId,
            id,
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Reativar_RepassaUsuarioIdETokenERetornaNoContent()
    {
        using var cts = new CancellationTokenSource();
        var id = Guid.NewGuid();

        var result = await _controller.Reativar(id, cts.Token);

        Assert.IsType<NoContentResult>(result);
        _reativar.Verify(c => c.ExecuteAsync(_userId, id, cts.Token), Times.Once);
    }

    [Theory]
    [InlineData("EntityNotFoundException")]
    [InlineData("CommandValidationException")]
    public async Task Encerrar_NaoEngoleExcecoesDoComando(string tipoExcecao)
    {
        Exception excecao = tipoExcecao == "EntityNotFoundException"
            ? new EntityNotFoundException("Movimento não encontrado")
            : new CommandValidationException("Dados inválidos");
        _encerrar
            .Setup(c => c.ExecuteAsync(
                It.IsAny<MovimentoModelForEncerramento>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(excecao);

        await Assert.ThrowsAsync(excecao.GetType(), () =>
            _controller.Encerrar(Guid.NewGuid(), new MovimentoForEncerramentoDto(), CancellationToken.None));
    }

    [Fact]
    public void PossuiFiltroDeValidacaoDeRequisicao()
    {
        Assert.Contains(
            typeof(RequestValidationAttribute),
            typeof(EncerramentoMovimentoController).GetCustomAttributes(true).Select(a => a.GetType()));
    }
}
