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

    bool EstaAtivo,

    [NotEmptyGuid]
    Guid UserId) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Meta < 1m || Meta > 9999999999999999.99m)
            yield return new ValidationResult(
                "A meta deve ser maior ou igual a 1 e ter no máximo 16 dígitos inteiros",
                new[] { nameof(Meta) });

        if (decimal.Round(Meta, 2) != Meta)
            yield return new ValidationResult(
                "A meta deve ter no máximo 2 casas decimais",
                new[] { nameof(Meta) });

        if (AporteMensal > 9999999999999999.99m)
            yield return new ValidationResult(
                "O aporte mensal deve ter no máximo 16 dígitos inteiros",
                new[] { nameof(AporteMensal) });

        if (decimal.Round(AporteMensal, 2) != AporteMensal)
            yield return new ValidationResult(
                "O aporte mensal deve ter no máximo 2 casas decimais",
                new[] { nameof(AporteMensal) });
    }

    public Objetivo ToDomain() => new()
    {
        Nome = Nome,
        Descricao = Descricao,
        Meta = Meta,
        FontePagadora = FontePagadora,
        AporteMensal = AporteMensal,
        OndeAplicar = OndeAplicar,
        EstaAtivo = EstaAtivo,
        UserId = UserId
    };
}
