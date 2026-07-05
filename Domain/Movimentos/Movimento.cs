using Domain.Corretoras;
using Domain.Emissores;
using Domain.Objetivos;
using Domain.Produtos;
using Domain.TiposRenda;
using Domain.ValueObjects;
using LumiaFoundation.EFRepository.Domain;

namespace Domain.Movimentos
{
    public class Movimento : Entity
    {
        public Objetivo Objetivo { get; private set; }
        public TipoRenda TipoRenda { get; private set; }
        public Corretora Corretora { get; private set; }
        public Produto Produto { get; private set; }
        public Emissor Emissor { get; private set; }
        public string? RentabilidadeContratada { get; private set; }
        public string? CotacaoNaCompra { get; private set; }
        public DataInvestimento DataInvestimento { get; private set; }
        public ValorAporte ValorAporte { get; private set; }
        public bool EhReinvestimento { get; private set; }
        public bool EstaAtivo { get; private set; }
        public ValorLiquidoAtual ValorLiquidoAtual { get; private set; }
    }
}