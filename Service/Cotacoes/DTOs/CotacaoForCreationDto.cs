using Application.Cotacoes.Commands.CreateCotacao;

namespace Service.Cotacoes.DTOs;

public record CotacaoForCreationDto(string? Papel, decimal Valor, DateTime Data)
{
    public CotacaoModelForCreation ToCreateCotacaoCommand() => new()
    {
        Papel = Papel,
        Valor = Valor,
        Data = Data
    };
}
