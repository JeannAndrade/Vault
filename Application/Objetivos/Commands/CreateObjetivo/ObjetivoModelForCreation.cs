using System.ComponentModel.DataAnnotations;
using Domain.Objetivos;
using LumiaFoundation.Core.ValidationAttributes;

namespace Application.Objetivos.Commands.CreateObjetivo;

public record ObjetivoModelForCreation(
    [property: Required(ErrorMessage = "Nome do Objetivo é obrigatório")]
    [property: MaxLength(100, ErrorMessage = "Nome do Objetivo não deve exceder 100 caracteres")]
    string Nome,

    [property: MaxLength(500, ErrorMessage = "Descrição do Objetivo não deve exceder 500 caracteres")]
    string? Descricao,

    [property: Required(ErrorMessage = "Meta é obrigatória")]
    [Range(1, double.MaxValue, ErrorMessage = "O valor deve ser positivo")]
    decimal Meta,

    [property: Required(ErrorMessage = "Fonte pagadora é obrigatória")]
    [property: MaxLength(100, ErrorMessage = "Fonte pagadora não deve exceder 100 caracteres")]
    string FontePagadora,

    [property: Required(ErrorMessage = "Aporte mensal é obrigatório")]
    [Range(1, double.MaxValue, ErrorMessage = "O valor deve ser positivo")]
    decimal AporteMensal,

    [property: Required(ErrorMessage = "Onde aplicar é obrigatório")]
    [property: MaxLength(100, ErrorMessage = "Onde aplicar não deve exceder 100 caracteres")]
    string OndeAplicar,

    [NotEmptyGuid]
    Guid UserId)
{
    public Objetivo ToDomain() => new()
    {
        Nome = Nome,
        Descricao = Descricao,
        Meta = Meta,
        FontePagadora = FontePagadora,
        AporteMensal = AporteMensal,
        OndeAplicar = OndeAplicar,
        UserId = UserId
    };
}
