using System.Net;
using LumiaFoundation.Abstractions.Pagination;
using LumiaFoundation.Http.Client.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;
using Presentation.Movimentos;
using Presentation.Pages.Movimentos;

namespace Presentation.Test.Pages.Movimentos;

public class IndexModelTests
{
    private readonly Mock<IMovimentoApi> _movimentoApi = new();

    private IndexModel CriarModelo(int pagina = 1) => new(_movimentoApi.Object) { Pagina = pagina };

    private void ConfigurarPagina(int pagina, PagedResponse<MovimentoDto> resposta) =>
        _movimentoApi
            .Setup(a => a.GetPagedAsync(pagina, IndexModel.TamanhoPagina, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resposta);

    private static PagedResponse<MovimentoDto> Pagina(int pagina, int totalPaginas, int totalItens, params MovimentoDto[] itens) =>
        new(itens, pagina, IndexModel.TamanhoPagina, totalItens, totalPaginas);

    [Fact]
    public async Task OnGetAsync_CarregaAPaginaSolicitada()
    {
        var esperada = Pagina(2, 3, 41, new MovimentoDto { Id = Guid.NewGuid() });
        ConfigurarPagina(2, esperada);
        var model = CriarModelo(pagina: 2);

        var result = await model.OnGetAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.Same(esperada, model.Resultado);
        Assert.Same(esperada.Items, model.Movimentos);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-4)]
    public async Task OnGetAsync_ComPaginaInvalida_UsaAPrimeiraPagina(int paginaInvalida)
    {
        ConfigurarPagina(1, Pagina(1, 1, 0));
        var model = CriarModelo(pagina: paginaInvalida);

        var result = await model.OnGetAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        _movimentoApi.Verify(a => a.GetPagedAsync(1, IndexModel.TamanhoPagina, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnGetAsync_QuandoAPaginaNaoExisteMais_RedirecionaParaAUltima()
    {
        ConfigurarPagina(5, Pagina(5, totalPaginas: 2, totalItens: 25));
        var model = CriarModelo(pagina: 5);

        var result = await model.OnGetAsync(CancellationToken.None);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(2, redirect.RouteValues!["pagina"]);
    }

    [Fact]
    public async Task OnGetAsync_SemMovimentos_NaoRedireciona()
    {
        ConfigurarPagina(1, Pagina(1, totalPaginas: 0, totalItens: 0));
        var model = CriarModelo();

        var result = await model.OnGetAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.Empty(model.Movimentos);
    }

    [Fact]
    public async Task OnPostDeleteAsync_WhenApiSucceeds_ReloadsSamePageWithoutError()
    {
        ConfigurarPagina(2, Pagina(2, 3, 41, new MovimentoDto { Id = Guid.NewGuid() }));
        var model = CriarModelo(pagina: 2);
        var id = Guid.NewGuid();

        var result = await model.OnPostDeleteAsync(id, CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.Null(model.ErrorMessage);
        _movimentoApi.Verify(a => a.DeleteAsync(id, It.IsAny<CancellationToken>()), Times.Once);
        _movimentoApi.Verify(a => a.GetPagedAsync(2, IndexModel.TamanhoPagina, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostDeleteAsync_WhenApiThrows_ShowsErrorMessageAndReloadsPage()
    {
        _movimentoApi
            .Setup(a => a.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ApiException(HttpStatusCode.InternalServerError, "erro"));
        ConfigurarPagina(1, Pagina(1, 1, 0));
        var model = CriarModelo();

        var result = await model.OnPostDeleteAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.NotNull(model.ErrorMessage);
    }

    [Fact]
    public async Task OnPostDeleteAsync_WhenLastItemOfLastPageIsDeleted_RedirectsToNewLastPage()
    {
        ConfigurarPagina(3, Pagina(3, totalPaginas: 2, totalItens: 40));
        var model = CriarModelo(pagina: 3);

        var result = await model.OnPostDeleteAsync(Guid.NewGuid(), CancellationToken.None);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(2, redirect.RouteValues!["pagina"]);
    }
}
