using System.ComponentModel.DataAnnotations;
using Domain.Objetivos;
using Domain.ValueObjects;

namespace Application.Objetivos.Commands.CreateObjetivo;

public record ObjetivoModelForCreation(
    [property: Required(ErrorMessage = "Nome do Objetivo é obrigatório")]
    [property: MaxLength(100, ErrorMessage = "Nome do Objetivo não deve exceder 100 caracteres")]
    string Nome,

    string? Descricao,

    [property: Required(ErrorMessage = "Meta é obrigatória")]
    decimal Meta,

    [property: Required(ErrorMessage = "Fonte pagadora é obrigatória")]
    [property: MaxLength(100, ErrorMessage = "Fonte pagadora não deve exceder 100 caracteres")]
    string FontePagadora,

    [property: Required(ErrorMessage = "Aporte mensal é obrigatório")]
    decimal AporteMensal,

    [property: Required(ErrorMessage = "Onde aplicar é obrigatório")]
    [property: MaxLength(100, ErrorMessage = "Onde aplicar não deve exceder 100 caracteres")]
    string OndeAplicar,

    [property: Required(ErrorMessage = "O usuário é obrigatório")]
    Guid UserId)
{
  public Objetivo ToDomain() => new()
  {
    Nome = Nome,
    Descricao = Descricao,
    Meta = new Meta(Meta),
    FontePagadora = FontePagadora,
    AporteMensal = new AporteMensal(AporteMensal),
    OndeAplicar = OndeAplicar,
    UserId = UserId
  };
}
