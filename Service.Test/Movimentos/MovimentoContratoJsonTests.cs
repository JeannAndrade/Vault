using System.Text.Json;
using Domain.Movimentos;
using Service.Movimentos.DTOs;

namespace Service.Test.Movimentos;

public class MovimentoContratoJsonTests
{
    private static readonly JsonSerializerOptions OpcoesJson = new(JsonSerializerDefaults.Web);

    [Fact]
    public void MovimentoDto_SerializaMotivoComoTextoENuloQuandoAtivo()
    {
        var encerrado = JsonSerializer.Serialize(
            new MovimentoDto { MotivoEncerramento = MotivoEncerramento.Gasto },
            OpcoesJson);
        var ativo = JsonSerializer.Serialize(new MovimentoDto(), OpcoesJson);

        Assert.Contains("\"motivoEncerramento\":\"Gasto\"", encerrado, StringComparison.Ordinal);
        Assert.Contains("\"motivoEncerramento\":null", ativo, StringComparison.Ordinal);
    }

    [Fact]
    public void MovimentoForEncerramentoDto_DesserializaEnumTexto()
    {
        var dto = JsonSerializer.Deserialize<MovimentoForEncerramentoDto>(
            """{"motivo":"Pendente","data":"2026-10-20"}""",
            OpcoesJson);

        Assert.NotNull(dto);
        Assert.Equal(MotivoEncerramento.Pendente, dto.Motivo);
        Assert.Equal(new DateTime(2026, 10, 20), dto.Data);
    }

    [Fact]
    public void MovimentoForEncerramentoDto_AceitaTextoSemDiferenciarMaiusculas()
    {
        var dto = JsonSerializer.Deserialize<MovimentoForEncerramentoDto>(
            """{"motivo":"gasto","data":"2026-10-20"}""",
            OpcoesJson);

        Assert.Equal(MotivoEncerramento.Gasto, dto!.Motivo);
    }

    [Fact]
    public void MovimentoForEncerramentoDto_TextoDesconhecidoLancaJsonException()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<MovimentoForEncerramentoDto>(
            """{"motivo":"Foo","data":"2026-10-20"}""",
            OpcoesJson));
    }

    [Fact]
    public void MovimentoForEncerramentoDto_SemMotivoPermaneceNulo()
    {
        var dto = JsonSerializer.Deserialize<MovimentoForEncerramentoDto>(
            """{"data":"2026-10-20"}""",
            OpcoesJson);

        Assert.NotNull(dto);
        Assert.Null(dto.Motivo);
    }

    [Fact]
    public void MovimentoForCreationDto_IgnoraEstaAtivoDoContratoAntigo()
    {
        var dto = JsonSerializer.Deserialize<MovimentoForCreationDto>(
            """{"estaAtivo":false}""",
            OpcoesJson);

        Assert.NotNull(dto);
        Assert.Null(typeof(MovimentoForCreationDto).GetProperty("EstaAtivo"));
        Assert.Null(dto.MotivoEncerramento);
        Assert.Null(dto.DataEncerramento);
    }
}
