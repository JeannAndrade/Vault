using LumiaFoundation.Core.Domain;

namespace Domain.Emissores;

public class Emissor : Entity
{
    public string Nome { get; set; } = string.Empty;

    public Guid UserId { get; set; }

    public User? User { get; set; }

    public static Emissor GetDefault()
    {
        return new Emissor
        {
            Nome = "Emissor Padrão",
            UserId = Guid.Empty
        };
    }
}
