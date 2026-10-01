using Moq;
using Presentation.Corretoras;
using Presentation.Emissores;
using Presentation.Movimentos;
using Presentation.Objetivos;
using Presentation.Produtos;
using Presentation.TiposRenda;

namespace Presentation.Test.Movimentos;

public class MovimentoFormLookupDataProviderTests
{
    [Fact]
    public async Task LoadAsync_ReturnsAllFiveListsAsSelectListItemsOrderedByNome()
    {
        var objetivoApi = new Mock<IObjetivoApi>();
        var tipoRendaApi = new Mock<ITipoRendaApi>();
        var corretoraApi = new Mock<ICorretoraApi>();
        var produtoApi = new Mock<IProdutoApi>();
        var emissorApi = new Mock<IEmissorApi>();

        var corretoraZ = new CorretoraDto(Guid.NewGuid(), "Zebra Corretora");
        var corretoraA = new CorretoraDto(Guid.NewGuid(), "Alfa Corretora");
        corretoraApi.Setup(a => a.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([corretoraZ, corretoraA]);

        objetivoApi.Setup(a => a.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        tipoRendaApi.Setup(a => a.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        produtoApi.Setup(a => a.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        emissorApi.Setup(a => a.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var provider = new MovimentoFormLookupDataProvider(
            objetivoApi.Object, tipoRendaApi.Object, corretoraApi.Object, produtoApi.Object, emissorApi.Object);

        var result = await provider.LoadAsync();

        Assert.Equal(2, result.Corretoras.Count);
        Assert.Equal("Alfa Corretora", result.Corretoras[0].Text);
        Assert.Equal("Zebra Corretora", result.Corretoras[1].Text);
        Assert.Equal(corretoraA.Id.ToString(), result.Corretoras[0].Value);
    }
}
