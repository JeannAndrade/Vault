using Domain.Objetivos;

namespace Application.Objetivos;

public record ObjetivoModel
{
  public Guid Id { get; set; }
  public string Nome { get; set; } = string.Empty;
  public string? Descricao { get; set; }
  public decimal Meta { get; set; }
  public string FontePagadora { get; set; } = string.Empty;
  public decimal AporteMensal { get; set; }
  public string OndeAplicar { get; set; } = string.Empty;
  public Guid UserId { get; set; }

  public static ObjetivoModel FromDomain(Objetivo objetivo)
  {
    return new ObjetivoModel
    {
      Id = objetivo.Id,
      Nome = objetivo.Nome,
      Descricao = objetivo.Descricao,
      Meta = objetivo.Meta.Value,
      FontePagadora = objetivo.FontePagadora,
      AporteMensal = objetivo.AporteMensal.Value,
      OndeAplicar = objetivo.OndeAplicar,
      UserId = objetivo.UserId
    };
  }
}
