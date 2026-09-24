using Application.Objetivos.Commands.CreateObjetivo;

namespace Service.Objetivos.DTOs;

public record ObjetivoForCreationDto(
    string Nome,
    string? Descricao,
    decimal Meta,
    string FontePagadora,
    decimal AporteMensal,
    string OndeAplicar)
{
  public ObjetivoModelForCreation ToCreateObjetivoCommand(Guid userId) => new(
      Nome,
      Descricao,
      Meta,
      FontePagadora,
      AporteMensal,
      OndeAplicar,
      userId);
}
