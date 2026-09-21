using LumiaFoundation.Core.Domain;

namespace Domain.Corretoras;

public class Corretora : Entity
{
    public string Nome { get; set; } = string.Empty;

    public Guid UserId { get; set; }

    public User? User { get; set; }
}
