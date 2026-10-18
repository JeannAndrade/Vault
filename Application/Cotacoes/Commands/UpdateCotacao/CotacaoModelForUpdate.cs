using System.ComponentModel.DataAnnotations;
using Domain.Cotacoes;

namespace Application.Cotacoes.Commands.UpdateCotacao;

public record CotacaoModelForUpdate
{
    [Required] [MaxLength(60)] public string? Papel { get; init; }
    [Range(typeof(decimal), "0.0001", "99999999999999.9999", ErrorMessage = "O valor da cotação deve ser maior que zero")] public decimal Valor { get; init; }
    [Required] public DateTime Data { get; init; }
    public void UpdateDomain(Cotacao c) { c.Papel = Papel!.Trim(); c.Valor = Valor; c.Data = Data.Date; }
}
