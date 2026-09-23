using Domain.TiposRenda;

namespace Application.TiposRenda;

public record TipoRendaModel
{
  public Guid Id { get; set; }
  public string Nome { get; set; } = string.Empty;
  public Guid UserId { get; set; }

  public static TipoRendaModel FromDomain(TipoRenda tipoRenda)
  {
    return new TipoRendaModel
    {
      Id = tipoRenda.Id,
      Nome = tipoRenda.Nome,
      UserId = tipoRenda.UserId
    };
  }
}
