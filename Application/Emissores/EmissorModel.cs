using Domain.Emissores;

namespace Application.Emissores;

public record EmissorModel
{
  public Guid Id { get; set; }
  public string Nome { get; set; } = string.Empty;
  public Guid UserId { get; set; }

  public static EmissorModel FromDomain(Emissor emissor)
  {
    return new EmissorModel
    {
      Id = emissor.Id,
      Nome = emissor.Nome,
      UserId = emissor.UserId
    };
  }
}
