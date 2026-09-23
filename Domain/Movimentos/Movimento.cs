using Domain.Corretoras;
using Domain.Emissores;
using Domain.Objetivos;
using Domain.Produtos;
using Domain.TiposRenda;
using LumiaFoundation.Core.Domain;

namespace Domain.Movimentos;

public class Movimento : Entity
{
    public Guid UserId { get; set; }
    public Guid ObjetivoId { get; set; }
    public Guid TipoRendaId { get; set; }
    public Guid CorretoraId { get; set; }
    public Guid ProdutoId { get; set; }
    public Guid EmissorId { get; set; }
    public string? RentabilidadeContratada { get; set; }
    public string? CotacaoNaCompra { get; set; }
    public DateTime DataInvestimento { get; set; }
    public decimal ValorAporte { get; set; } = 0M;
    public bool EhReinvestimento { get; set; }
    public bool EstaAtivo { get; set; }
    public decimal ValorLiquidoAtual { get; set; } = 0M;

    public User User { get; set; } = null!;
    public Objetivo Objetivo { get; set; } = null!;
    public TipoRenda TipoRenda { get; set; } = null!;
    public Corretora Corretora { get; set; } = null!;
    public Produto Produto { get; set; } = null!;
    public Emissor Emissor { get; set; } = null!;
}
