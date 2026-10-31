using System.Text.Json;
using Presentation.Dashboards;

namespace Presentation.Test.Dashboards;

public class ConsolidadoContratoJsonTests
{
    [Fact]
    public void DesserializaExemploEGruposAusentesViraramListaVazia()
    {
        var opcoes = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var dto = JsonSerializer.Deserialize<ConsolidadoDto>("""{"total":{"id":null,"nome":null,"quantidade":5,"totalAportado":900.00,"totalLiquido":1000.00,"percentualLiquido":1},"grupos":[{"id":"11111111-1111-1111-1111-111111111111","nome":"Inter","quantidade":3,"totalAportado":500.00,"totalLiquido":600.00,"percentualLiquido":0.6},{"id":"22222222-2222-2222-2222-222222222222","nome":"XP","quantidade":2,"totalAportado":400.00,"totalLiquido":400.00,"percentualLiquido":0.4}]}""", opcoes)!;
        Assert.Equal(5, dto.Total.Quantidade);
        Assert.Equal(0.6m, dto.Grupos[0].PercentualLiquido);
        var semGrupos = JsonSerializer.Deserialize<ConsolidadoDto>("{\"total\":{}}", opcoes)!;
        Assert.NotNull(semGrupos.Grupos);
        Assert.Empty(semGrupos.Grupos);
    }

    [Fact]
    public void NomesDaDimensaoSaoContratoEstavel() =>
        Assert.Equal(["TipoRenda", "Corretora", "Produto", "Emissor"], Enum.GetNames<DimensaoConsolidado>());
}
