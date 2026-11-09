using Domain.Corretoras;

namespace Application.Corretoras;

public record CorretoraModel
{
  public Guid Id { get; set; }
  public string Nome { get; set; } = string.Empty;
  public Guid OwnerId { get; set; }

  public static CorretoraModel FromDomain(Corretora corretora)
  {
    return new CorretoraModel
    {
      Id = corretora.Id,
      Nome = corretora.Nome,
      OwnerId = corretora.OwnerId
    };
  }
}
