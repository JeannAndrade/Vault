using Presentation.Pages.Dashboards;

namespace Presentation.Test.Pages.Dashboards;

public class ConsolidadoFiltroInputTests
{
    [Fact]
    public void HasValueECopiaFiltro() {
        var tipo = Guid.NewGuid();
        var input = new ConsolidadoFiltroInput { TipoRendaId = tipo, CorretoraId = Guid.NewGuid(), ProdutoId = Guid.NewGuid(), EmissorId = Guid.NewGuid() };
        Assert.True(input.HasValue);
        Assert.Equal(tipo, input.ToFiltro().TipoRendaId);
        Assert.False(new ConsolidadoFiltroInput().HasValue);
    }
}
