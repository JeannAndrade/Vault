using System.Text.Json;
using Presentation.Movimentos;

namespace Presentation.Test.Movimentos;

public class MovimentoContratoJsonTests
{
    private static readonly JsonSerializerOptions OpcoesJson = new(JsonSerializerDefaults.Web);

    [Fact]
    public void MovimentoForEncerramentoDto_SerializaMotivoTextoEData()
    {
        var json = JsonSerializer.Serialize(
            new MovimentoForEncerramentoDto
            {
                Motivo = MotivoEncerramento.Gasto,
                Data = new DateTime(2026, 10, 20)
            },
            OpcoesJson);

        Assert.Contains("\"motivo\":\"Gasto\"", json, StringComparison.Ordinal);
        Assert.Contains("\"data\":\"2026-10-20T00:00:00\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void MovimentoDto_DesserializaEncerramentoEAnulaveisAusentes()
    {
        var encerrado = JsonSerializer.Deserialize<MovimentoDto>(
            """{"motivoEncerramento":"Pendente","dataEncerramento":"2026-10-20"}""",
            OpcoesJson);
        var ausente = JsonSerializer.Deserialize<MovimentoDto>("{}", OpcoesJson);
        var nulo = JsonSerializer.Deserialize<MovimentoDto>(
            """{"motivoEncerramento":null,"dataEncerramento":null}""",
            OpcoesJson);

        Assert.Equal(MotivoEncerramento.Pendente, encerrado!.MotivoEncerramento);
        Assert.Equal(new DateTime(2026, 10, 20), encerrado.DataEncerramento);
        Assert.Null(ausente!.MotivoEncerramento);
        Assert.Null(ausente.DataEncerramento);
        Assert.Null(nulo!.MotivoEncerramento);
        Assert.Null(nulo.DataEncerramento);
    }

    [Fact]
    public void MovimentoForCreationDto_SerializaMotivoTextoESemEstaAtivo()
    {
        var json = JsonSerializer.Serialize(
            new MovimentoForCreationDto { MotivoEncerramento = MotivoEncerramento.Reinvestido },
            OpcoesJson);

        Assert.Contains("\"motivoEncerramento\":\"Reinvestido\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("estaAtivo", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Enum_ContemExatamenteOsNomesDoContratoDaApi()
    {
        Assert.Equal(["Gasto", "Reinvestido", "Pendente"], Enum.GetNames<MotivoEncerramento>());
    }

    [Theory]
    [InlineData(typeof(MovimentoForCreationDto))]
    [InlineData(typeof(MovimentoForUpdateDto))]
    public void DtosDeCriacaoEEdicaoNaoPossuemEstaAtivo(Type tipoDto) =>
        Assert.Null(tipoDto.GetProperty("EstaAtivo"));
}
