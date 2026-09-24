using Application.Objetivos.Commands.UpdateObjetivo;

namespace Service.Objetivos.DTOs;

public record ObjetivoForUpdateDto(
    string Nome,
    string? Descricao,
    decimal Meta,
    string FontePagadora,
    decimal AporteMensal,
    string OndeAplicar)
{
  public ObjetivoModelForUpdate ToUpdateObjetivoCommand() => new()
  {
    Nome = Nome,
    Descricao = Descricao,
    Meta = Meta,
    FontePagadora = FontePagadora,
    AporteMensal = AporteMensal,
    OndeAplicar = OndeAplicar
  };
}
