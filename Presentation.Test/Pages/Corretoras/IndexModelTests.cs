using Moq;
using Presentation.Corretoras;
using Presentation.Pages.Corretoras;

namespace Presentation.Test.Pages.Corretoras;

public class IndexModelTests
{
    [Fact]
    public async Task OnGetAsync_LoadsCorretorasFromApi()
    {
        var corretoraApi = new Mock<ICorretoraApi>();
        var expected = new List<CorretoraDto> { new(Guid.NewGuid(), "XP") };
        corretoraApi.Setup(a => a.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var model = new IndexModel(corretoraApi.Object);

        await model.OnGetAsync(CancellationToken.None);

        Assert.Same(expected, model.Corretoras);
    }
}
