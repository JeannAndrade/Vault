using System.ComponentModel.DataAnnotations;

namespace Application.Dashboards.Rentabilidade;

public record RentabilidadeParameters
{
    public const int TopPadrao = 25;
    public const int TopMaximo = 100;

    [Range(1, TopMaximo, ErrorMessage = "O top deve estar entre {1} e {2}")]
    public int Top { get; init; } = TopPadrao;

    [EnumDataType(typeof(OrdemRentabilidade))]
    public OrdemRentabilidade Ordem { get; init; } = OrdemRentabilidade.Maiores;
}
