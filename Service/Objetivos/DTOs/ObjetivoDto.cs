using Application.Objetivos;

namespace Service.Objetivos.DTOs;

public class ObjetivoDto
{
  public Guid Id { get; set; }
  public string Nome { get; set; } = string.Empty;
  public string? Descricao { get; set; }
  public decimal Meta { get; set; }
  public string FontePagadora { get; set; } = string.Empty;
  public decimal AporteMensal { get; set; }
  public string OndeAplicar { get; set; } = string.Empty;

  public static ObjetivoDto FromApplication(ObjetivoModel objetivo)
  {
    return new ObjetivoDto
    {
      Id = objetivo.Id,
      Nome = objetivo.Nome,
      Descricao = objetivo.Descricao,
      Meta = objetivo.Meta,
      FontePagadora = objetivo.FontePagadora,
      AporteMensal = objetivo.AporteMensal,
      OndeAplicar = objetivo.OndeAplicar
    };
  }

  public static List<ObjetivoDto> FromApplication(List<ObjetivoModel> objetivos)
  {
    return [.. objetivos.Select(FromApplication)];
  }
}
