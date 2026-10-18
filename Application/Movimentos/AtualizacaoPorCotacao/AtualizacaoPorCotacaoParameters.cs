using System.ComponentModel.DataAnnotations;

namespace Application.Movimentos.AtualizacaoPorCotacao;

public record AtualizacaoPorCotacaoParameters
{
    [Required]
    [MaxLength(60)]
    public string? Papel { get; init; }

    [Range(typeof(decimal), "0.0001", "99999999999999.9999", ErrorMessage = "O valor da cotação deve ser maior que zero")]
    public decimal Valor { get; init; }

    [Required]
    public DateTime Data { get; init; }
}
