using Domain.Bancos;
using LumiaFoundation.Core.Domain;

namespace Domain;

public class User : Entity
{
    public ICollection<Banco> Bancos { get; set; } = [];
}
