using Application.Emissores;

namespace Service.Emissores.DTOs;

public class EmissorDto
{
  public Guid Id { get; set; }
  public string Nome { get; set; } = string.Empty;

  public static EmissorDto FromApplication(EmissorModel emissor)
  {
    return new EmissorDto
    {
      Id = emissor.Id,
      Nome = emissor.Nome
    };
  }

  public static List<EmissorDto> FromApplication(List<EmissorModel> emissores)
  {
    return [.. emissores.Select(FromApplication)];
  }
}
