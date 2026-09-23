using Domain.Corretoras;
using Domain.Emissores;
using Domain.Objetivos;
using Domain.Produtos;
using Domain.TiposRenda;
using LumiaFoundation.Core.Domain;

namespace Domain.Movimentos;

public class Movimento : Entity
{
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

    public Guid UserId { get; set; }
    public User? User { get; set; }
    public Objetivo Objetivo { get; set; } = Objetivo.GetDefault();
    public TipoRenda TipoRenda { get; set; } = TipoRenda.GetDefault();
    public Corretora Corretora { get; set; } = Corretora.GetDefault();
    public Produto Produto { get; set; } = Produto.GetDefault();
    public Emissor Emissor { get; set; } = Emissor.GetDefault();
}
