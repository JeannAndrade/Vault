using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;
using Presentation.Movimentos;
using Presentation.Pages.Movimentos;
using LumiaFoundation.Core.TimeService;
using System.Net;
using LumiaFoundation.Http.Client.Exceptions;

namespace Presentation.Test.Pages.Movimentos;

public class CreateModelTests
{
    private readonly Mock<IMovimentoApi> _movimentoApi = new();
    private readonly Mock<IMovimentoFormLookupData> _lookupData = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly DateTime _hoje = new(2026, 10, 27, 15, 0, 0, DateTimeKind.Utc);

    public CreateModelTests()
    {
        _lookupData
            .Setup(l => l.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MovimentoFormLookupData([], [], [], [], []));
        _clock.Setup(c => c.GetDateTime()).Returns(_hoje);
    }

    private CreateModel CriarModelo() => new(_movimentoApi.Object, _lookupData.Object, _clock.Object);

    private CreateModel.InputModel ValidInput() => new()
    {
        ObjetivoId = Guid.NewGuid(),
        TipoRendaId = Guid.NewGuid(),
        CorretoraId = Guid.NewGuid(),
        ProdutoId = Guid.NewGuid(),
        EmissorId = Guid.NewGuid(),
        DataInvestimento = DateTime.Today,
        ValorAporte = 1000m,
        ValorLiquidoAtual = 1000m
    };

    [Fact]
    public async Task OnGetAsync_DefaultsDataInvestimentoToTodayAndLeavesClosureEmpty()
    {
        var model = CriarModelo();

        await model.OnGetAsync(CancellationToken.None);

        Assert.Equal(DateTime.Today, model.Input.DataInvestimento);
        Assert.Null(model.Encerramento.Motivo);
        Assert.Null(model.Encerramento.Data);
    }

    [Fact]
    public async Task OnPostAsync_WhenModelStateIsInvalid_ReturnsPageWithoutCallingApi()
    {
        var model = CriarModelo();
        model.Input = ValidInput();
        model.ModelState.AddModelError("Input.ObjetivoId", "Selecione o objetivo.");

        var result = await model.OnPostAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        _movimentoApi.Verify(a => a.CreateAsync(It.IsAny<MovimentoForCreationDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_WhenApiSucceeds_RedirectsToIndex()
    {
        var model = CriarModelo();
        model.Input = ValidInput();

        var result = await model.OnPostAsync(CancellationToken.None);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("Index", redirect.PageName);
        Assert.Equal(1, redirect.RouteValues!["pagina"]);   // sem página de origem, volta para a primeira
    }

    [Fact]
    public async Task OnPostAsync_WhenApiSucceeds_RedirectsToIndexKeepingOriginPage()
    {
        var model = CriarModelo();
        model.Input = ValidInput();
        model.Pagina = 4;

        var result = await model.OnPostAsync(CancellationToken.None);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("Index", redirect.PageName);
        Assert.Equal(4, redirect.RouteValues!["pagina"]);
    }

    [Fact]
    public async Task OnPostAsync_WhenApiSucceeds_RedirectsToIndexKeepingFilterAndPage()
    {
        var objetivoId = Guid.NewGuid();
        var model = CriarModelo();
        model.Input = ValidInput();
        model.Pagina = 4;
        model.Filtro = new MovimentoFiltroInput { ObjetivoId = objetivoId, EstaAtivo = true };

        var result = await model.OnPostAsync(CancellationToken.None);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(objetivoId, redirect.RouteValues!["Filtro.ObjetivoId"]);
        Assert.Equal("true", redirect.RouteValues["Filtro.EstaAtivo"]);
        Assert.Equal(4, redirect.RouteValues["pagina"]);
    }

    [Fact]
    public async Task OnPostAsync_SemEncerramento_EnviaCamposNulos()
    {
        var model = CriarModelo();
        model.Input = ValidInput();

        await model.OnPostAsync(CancellationToken.None);

        _movimentoApi.Verify(a => a.CreateAsync(
            It.Is<MovimentoForCreationDto>(dto => dto.MotivoEncerramento == null && dto.DataEncerramento == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_ComEncerramentoValido_EnviaMotivoEData()
    {
        var data = _hoje.Date.AddDays(-1);
        var model = CriarModelo();
        model.Input = ValidInput();
        model.Encerramento = new EncerramentoInput { Motivo = MotivoEncerramento.Gasto, Data = data };

        await model.OnPostAsync(CancellationToken.None);

        _movimentoApi.Verify(a => a.CreateAsync(
            It.Is<MovimentoForCreationDto>(dto => dto.MotivoEncerramento == MotivoEncerramento.Gasto && dto.DataEncerramento == data),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(false, true, false, "Encerramento.Motivo")]
    [InlineData(true, false, false, "Encerramento.Data")]
    [InlineData(true, true, true, "Encerramento.Data")]
    public async Task OnPostAsync_ComEncerramentoIncompletoOuFuturo_NaoChamaApi(
        bool possuiMotivo,
        bool possuiData,
        bool dataFutura,
        string campoComErro)
    {
        var model = CriarModelo();
        model.Input = ValidInput();
        model.Encerramento = new EncerramentoInput
        {
            Motivo = possuiMotivo ? MotivoEncerramento.Pendente : null,
            Data = dataFutura ? _hoje.Date.AddDays(1) : possuiData ? _hoje.Date : null
        };

        var result = await model.OnPostAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.Contains(model.ModelState, item => item.Key == campoComErro && item.Value!.Errors.Count > 0);
        _movimentoApi.Verify(a => a.CreateAsync(It.IsAny<MovimentoForCreationDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_QuandoApiRetorna422_MostraMensagemDaApi()
    {
        _movimentoApi
            .Setup(a => a.CreateAsync(It.IsAny<MovimentoForCreationDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ApiException(HttpStatusCode.UnprocessableEntity, "Mensagem da Api"));
        var model = CriarModelo();
        model.Input = ValidInput();

        var result = await model.OnPostAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.Equal("Mensagem da Api", model.ErrorMessage);
    }
}
