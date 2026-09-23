using LumiaFoundation.Core.Domain;

namespace Domain.TiposRenda;

public class TipoRenda : Entity
{
    public string Nome { get; set; } = string.Empty;

    public Guid UserId { get; set; }

    public User? User { get; set; }
}
