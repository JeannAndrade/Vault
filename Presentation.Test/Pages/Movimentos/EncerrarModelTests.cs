using System.Net;
using LumiaFoundation.Http.Client.Exceptions;
using LumiaFoundation.Core.TimeService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;
using Presentation.Movimentos;
using Presentation.Pages.Movimentos;

namespace Presentation.Test.Pages.Movimentos;

public class EncerrarModelTests
{
    private readonly Mock<IMovimentoApi> _movimentoApi = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly DateTime _hoje = new(2026, 10, 27, 16, 0, 0, DateTimeKind.Utc);
    private readonly Guid _id = Guid.NewGuid();

    private EncerrarModel CriarModelo() => new(_movimentoApi.Object, _clock.Object)
    {
        Id = _id,
        Pagina = 3,
        Filtro = new MovimentoFiltroInput { EstaAtivo = false }
    };

    public EncerrarModelTests()
    {
        _clock.Setup(c => c.GetDateTime()).Returns(_hoje);
    }

    [Fact]
    public async Task OnGetAsync_Ativo_PreencheDataDeHojeSemMotivo()
    {
        _movimentoApi.Setup(a => a.GetByIdAsync(_id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MovimentoDto { Id = _id, EstaAtivo = true });
        var model = CriarModelo();

        await model.OnGetAsync(CancellationToken.None);

        Assert.Equal(_hoje.Date, model.Input.Data);
        Assert.Null(model.Input.Motivo);
        Assert.False(model.EstaEncerrado);
    }

    [Fact]
    public async Task OnGetAsync_Encerrado_PreencheMotivoEDataAtuais()
    {
        var data = _hoje.Date.AddDays(-2);
        _movimentoApi.Setup(a => a.GetByIdAsync(_id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MovimentoDto
            {
                Id = _id,
                EstaAtivo = false,
                MotivoEncerramento = MotivoEncerramento.Pendente,
                DataEncerramento = data
            });
        var model = CriarModelo();

        await model.OnGetAsync(CancellationToken.None);

        Assert.Equal(MotivoEncerramento.Pendente, model.Input.Motivo);
        Assert.Equal(data, model.Input.Data);
        Assert.True(model.EstaEncerrado);
    }

    [Theory]
    [InlineData(false, true, false, "Input.Motivo")]
    [InlineData(true, false, false, "Input.Data")]
    [InlineData(true, true, true, "Input.Data")]
    public async Task OnPostAsync_ComCamposAusentesOuDataFutura_NaoChamaApi(
        bool possuiMotivo,
        bool possuiData,
        bool dataFutura,
        string campoEsperado)
    {
        var movimento = new MovimentoDto { Id = _id, EstaAtivo = true };
        ConfigurarCarregamento(movimento);
        var model = CriarModelo();
        model.Input = new EncerramentoInput
        {
            Motivo = possuiMotivo ? MotivoEncerramento.Gasto : null,
            Data = dataFutura ? _hoje.Date.AddDays(1) : possuiData ? _hoje.Date : null
        };

        var result = await model.OnPostAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.Contains(model.ModelState, item => item.Key == campoEsperado && item.Value!.Errors.Count > 0);
        _movimentoApi.Verify(a => a.EncerrarAsync(It.IsAny<Guid>(), It.IsAny<MovimentoForEncerramentoDto>(), It.IsAny<CancellationToken>()), Times.Never);
        _movimentoApi.Verify(a => a.GetByIdAsync(_id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_Valido_EnviaMotivoDataTokenERedirecionaComFiltroEPagina()
    {
        var data = _hoje.Date;
        ConfigurarCarregamento(new MovimentoDto { Id = _id, EstaAtivo = true });
        var model = CriarModelo();
        model.Input = new EncerramentoInput { Motivo = MotivoEncerramento.Gasto, Data = data };
        using var cts = new CancellationTokenSource();

        var result = await model.OnPostAsync(cts.Token);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("Index", redirect.PageName);
        Assert.Equal(3, redirect.RouteValues!["pagina"]);
        Assert.Equal("false", redirect.RouteValues["Filtro.EstaAtivo"]);
        _movimentoApi.Verify(a => a.EncerrarAsync(
            _id,
            It.Is<MovimentoForEncerramentoDto>(dto => dto.Motivo == MotivoEncerramento.Gasto && dto.Data == data),
            cts.Token), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_422ExibeMensagemDaApi()
    {
        ConfigurarCarregamento(new MovimentoDto { Id = _id, EstaAtivo = true });
        _movimentoApi.Setup(a => a.EncerrarAsync(_id, It.IsAny<MovimentoForEncerramentoDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ApiException(HttpStatusCode.UnprocessableEntity, "Data inválida informada pela Api"));
        var model = CriarModelo();
        model.Input = new EncerramentoInput { Motivo = MotivoEncerramento.Gasto, Data = _hoje.Date };

        var result = await model.OnPostAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.Equal("Data inválida informada pela Api", model.ErrorMessage);
    }

    [Fact]
    public async Task OnPostAsync_OutroErroDaApiExibeMensagemGenerica()
    {
        ConfigurarCarregamento(new MovimentoDto { Id = _id, EstaAtivo = true });
        _movimentoApi.Setup(a => a.EncerrarAsync(_id, It.IsAny<MovimentoForEncerramentoDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ApiException(HttpStatusCode.InternalServerError, "erro interno"));
        var model = CriarModelo();
        model.Input = new EncerramentoInput { Motivo = MotivoEncerramento.Gasto, Data = _hoje.Date };

        await model.OnPostAsync(CancellationToken.None);

        Assert.Equal("Não foi possível salvar o encerramento agora. Tente novamente em instantes.", model.ErrorMessage);
    }

    [Fact]
    public async Task OnPostAsync_ReclassificaMovimentoEncerradoPeloMesmoFluxo()
    {
        var data = _hoje.Date.AddDays(-1);
        ConfigurarCarregamento(new MovimentoDto
        {
            Id = _id,
            EstaAtivo = false,
            MotivoEncerramento = MotivoEncerramento.Pendente,
            DataEncerramento = data
        });
        var model = CriarModelo();
        model.Input = new EncerramentoInput { Motivo = MotivoEncerramento.Reinvestido, Data = data };

        var result = await model.OnPostAsync(CancellationToken.None);

        Assert.IsType<RedirectToPageResult>(result);
        _movimentoApi.Verify(a => a.EncerrarAsync(
            _id,
            It.Is<MovimentoForEncerramentoDto>(dto => dto.Motivo == MotivoEncerramento.Reinvestido && dto.Data == data),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostReativarAsync_SucessoReativaComTokenERedireciona()
    {
        var model = CriarModelo();
        using var cts = new CancellationTokenSource();

        var result = await model.OnPostReativarAsync(cts.Token);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("Index", redirect.PageName);
        Assert.Equal(3, redirect.RouteValues!["pagina"]);
        _movimentoApi.Verify(a => a.ReativarAsync(_id, cts.Token), Times.Once);
    }

    [Fact]
    public async Task OnPostReativarAsync_FalhaMostraMensagemERecarregaMovimento()
    {
        var movimento = new MovimentoDto { Id = _id, EstaAtivo = false };
        _movimentoApi.Setup(a => a.ReativarAsync(_id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ApiException(HttpStatusCode.InternalServerError, "erro"));
        ConfigurarCarregamento(movimento);
        var model = CriarModelo();

        var result = await model.OnPostReativarAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.Equal("Não foi possível reativar o movimento agora. Tente novamente em instantes.", model.ErrorMessage);
        Assert.Same(movimento, model.Movimento);
    }

    private void ConfigurarCarregamento(MovimentoDto movimento) =>
        _movimentoApi.Setup(a => a.GetByIdAsync(_id, It.IsAny<CancellationToken>())).ReturnsAsync(movimento);
}
