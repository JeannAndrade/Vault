using Domain.Movimentos;
using LumiaFoundation.Core.Domain;

namespace Domain.Emissores;

public class Emissor : Entity
{
    public string Nome { get; set; } = string.Empty;

    public Guid OwnerId { get; set; }

    public ICollection<Movimento> Movimentos { get; set; } = [];
}
