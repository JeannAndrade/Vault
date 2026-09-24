using Application.TiposRenda;

namespace Service.TiposRenda.DTOs;

public class TipoRendaDto
{
  public Guid Id { get; set; }
  public string Nome { get; set; } = string.Empty;

  public static TipoRendaDto FromApplication(TipoRendaModel tipoRenda)
  {
    return new TipoRendaDto
    {
      Id = tipoRenda.Id,
      Nome = tipoRenda.Nome
    };
  }

  public static List<TipoRendaDto> FromApplication(List<TipoRendaModel> tiposRenda)
  {
    return [.. tiposRenda.Select(FromApplication)];
  }
}
