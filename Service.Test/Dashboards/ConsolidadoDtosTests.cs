using System.Text.Json;
using Application.Dashboards.Consolidado;
using Persistence.Movimentos;
using Service.Dashboards.DTOs;

namespace Service.Test.Dashboards;

public class ConsolidadoDtosTests
{
    [Fact]
    public void FiltroSemArgumentosTemTudoNulo()
    {
        var filtro = new ConsolidadoFiltroDto();
        var parametros = filtro.ToParameters();
        Assert.Null(parametros.TipoRendaId);
        Assert.Null(parametros.CorretoraId);
        Assert.Null(parametros.ProdutoId);
        Assert.Null(parametros.EmissorId);
        Assert.Null(parametros.AgruparPor);
    }

    [Fact]
    public void FiltroCopiaTodosOsCampos()
    {
        var filtro = new ConsolidadoFiltroDto
        {
            TipoRendaId = Guid.NewGuid(), CorretoraId = Guid.NewGuid(), ProdutoId = Guid.NewGuid(),
            EmissorId = Guid.NewGuid(), AgruparPor = DimensaoConsolidado.Emissor
        };
        var parametros = filtro.ToParameters();
        Assert.Equal(filtro.TipoRendaId, parametros.TipoRendaId);
        Assert.Equal(filtro.CorretoraId, parametros.CorretoraId);
        Assert.Equal(filtro.ProdutoId, parametros.ProdutoId);
        Assert.Equal(filtro.EmissorId, parametros.EmissorId);
        Assert.Equal(filtro.AgruparPor, parametros.AgruparPor);
    }

    [Fact]
    public void FromApplicationPreservaValoresOrdemENulos()
    {
        var modelo = new ConsolidadoModel(
            new ConsolidadoLinhaModel(null, null, 5, 900.00m, 1000.00m, 1m),
            [new ConsolidadoLinhaModel(Guid.NewGuid(), "Inter", 3, 500m, 600m, 0.325m),
             new ConsolidadoLinhaModel(Guid.NewGuid(), "XP", 2, 400m, 400m, 0.4m)]);
        var dto = ConsolidadoDto.FromApplication(modelo);
        Assert.Null(dto.Total.Id);
        Assert.Null(dto.Total.Nome);
        Assert.Equal(modelo.Total.PercentualLiquido, dto.Total.PercentualLiquido);
        Assert.Equal(["Inter", "XP"], dto.Grupos.Select(g => g.Nome));
        Assert.Equal(0.325m, dto.Grupos[0].PercentualLiquido);
    }

    [Fact]
    public void ContratoJsonUsaNomesCorretosNumerosEIdaEVolta()
    {
        var dto = new ConsolidadoDto(
            new ConsolidadoLinhaDto(null, null, 5, 900m, 1000m, 1m),
            [new ConsolidadoLinhaDto(Guid.NewGuid(), "Inter", 3, 500m, 600m, 0.325m)]);
        var opcoes = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var json = JsonSerializer.Serialize(dto, opcoes);
        Assert.Contains("\"total\"", json);
        Assert.Contains("\"grupos\"", json);
        Assert.Contains("\"percentualLiquido\":0.325", json);
        Assert.Contains("\"id\":null", json);
        Assert.DoesNotContain("\"0.325\"", json);
        var reidratado = JsonSerializer.Deserialize<ConsolidadoDto>(json, opcoes)!;
        Assert.Equal(dto.Total.TotalLiquido, reidratado.Total.TotalLiquido);
        Assert.Equal(dto.Grupos[0].PercentualLiquido, reidratado.Grupos[0].PercentualLiquido);
    }
}
