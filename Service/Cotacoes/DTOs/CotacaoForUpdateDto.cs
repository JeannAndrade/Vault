using Application.Cotacoes.Commands.UpdateCotacao;

namespace Service.Cotacoes.DTOs;

public record CotacaoForUpdateDto(string? Papel, decimal Valor, DateTime Data)
{
    public CotacaoModelForUpdate ToUpdateCotacaoCommand() => new()
    {
        Papel = Papel,
        Valor = Valor,
        Data = Data
    };
}
