using Domain.Movimentos;
using LumiaFoundation.Core.Domain;

namespace Domain.Produtos;

public class Produto : Entity
{
    public string Nome { get; set; } = string.Empty;

    public Guid OwnerId { get; set; }

    public ICollection<Movimento> Movimentos { get; set; } = [];
}
