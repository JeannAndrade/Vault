using Domain.Bancos;
using Domain.Corretoras;
using Domain.Emissores;
using LumiaFoundation.Core.Domain;

namespace Domain;

public class User : Entity
{
    public ICollection<Banco> Bancos { get; set; } = [];
    public ICollection<Corretora> Corretoras { get; set; } = [];
    public ICollection<Emissor> Emissores { get; set; } = [];
}
