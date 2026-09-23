using LumiaFoundation.Core.Domain;

namespace Domain.Produtos;

public class Produto : Entity
{
    public string Nome { get; set; } = string.Empty;

    public Guid UserId { get; set; }

    public User? User { get; set; }

    public static Produto GetDefault()
    {
        return new Produto
        {
            Nome = "Produto Padrão",
            UserId = Guid.Empty
        };
    }
}
