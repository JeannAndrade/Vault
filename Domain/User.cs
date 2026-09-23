using Domain.Corretoras;
using Domain.Emissores;
using Domain.Movimentos;
using Domain.Objetivos;
using Domain.Produtos;
using Domain.TiposRenda;
using LumiaFoundation.Core.Domain;

namespace Domain;

public class User : Entity
{
    public ICollection<Corretora> Corretoras { get; set; } = [];
    public ICollection<Emissor> Emissores { get; set; } = [];
    public ICollection<Produto> Produtos { get; set; } = [];
    public ICollection<Objetivo> Objetivos { get; set; } = [];
    public ICollection<TipoRenda> TiposRenda { get; set; } = [];
    public ICollection<Movimento> Movimentos { get; set; } = [];
}
