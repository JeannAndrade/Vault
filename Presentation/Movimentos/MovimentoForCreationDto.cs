using System.Text.Json.Serialization;

namespace Presentation.Movimentos;

public sealed record MovimentoForCreationDto
{
    public Guid ObjetivoId { get; init; }
    public Guid TipoRendaId { get; init; }
    public Guid CorretoraId { get; init; }
    public Guid ProdutoId { get; init; }
    public Guid EmissorId { get; init; }
    public string? RentabilidadeContratada { get; init; }
    public string? CotacaoNaCompra { get; init; }
    public string? Observacao { get; init; }
    public string? Protocolo { get; init; }
    public decimal? Quantidade { get; init; }
    public DateTime DataInvestimento { get; init; }
    public DateTime? DataVencimento { get; init; }
    public decimal ValorAporte { get; init; }
    public bool EhReinvestimento { get; init; }
    [JsonConverter(typeof(JsonStringEnumConverter<MotivoEncerramento>))]
    public MotivoEncerramento? MotivoEncerramento { get; init; }
    public DateTime? DataEncerramento { get; init; }
    public decimal ValorLiquidoAtual { get; init; }
    public bool AtualizaPorCotacao { get; init; }
}
