using Application.Objetivos.Commands.CreateObjetivo;

namespace Service.Objetivos.DTOs;

public record ObjetivoForCreationDto(
    string Nome,
    string? Descricao,
    decimal Meta,
    string FontePagadora,
    decimal AporteMensal,
    string OndeAplicar,
    bool EstaAtivo)
{
  public ObjetivoModelForCreation ToCreateObjetivoCommand(Guid ownerId) => new(
      Nome,
      Descricao,
      Meta,
      FontePagadora,
      AporteMensal,
      OndeAplicar,
      EstaAtivo,
      ownerId);
}
