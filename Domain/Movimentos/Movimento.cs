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
    public string? Observacao { get; set; }
    public string? Protocolo { get; set; }
    public decimal? Quantidade { get; set; }
    public DateTime DataInvestimento { get; set; }
    public DateTime? DataVencimento { get; set; }
    public decimal ValorAporte { get; set; } = 0M;
    public bool EhReinvestimento { get; set; }
    public bool EstaAtivo { get; set; }
    public MotivoEncerramento? MotivoEncerramento { get; private set; }
    public DateTime? DataEncerramento { get; private set; }
    public decimal ValorLiquidoAtual { get; set; } = 0M;
    public DateTime DataAtualizacaoValorLiquido { get; set; }
    public bool AtualizaPorCotacao { get; set; }
    public Objetivo Objetivo { get; set; } = null!;
    public TipoRenda TipoRenda { get; set; } = null!;
    public Corretora Corretora { get; set; } = null!;
    public Produto Produto { get; set; } = null!;
    public Emissor Emissor { get; set; } = null!;

    public void AplicarCotacao(decimal cotacao, DateTime hoje)
    {
        if (Quantidade is null || Quantidade <= 0)
            throw new InvalidOperationException("A quantidade deve ser maior que zero para aplicar uma cotação.");

        ValorLiquidoAtual = decimal.Round(Quantidade.Value * cotacao, 2, MidpointRounding.AwayFromZero);
        DataAtualizacaoValorLiquido = hoje.Date;
    }

    public void Encerrar(MotivoEncerramento motivo, DateTime data, DateTime hoje)
    {
        if (!Enum.IsDefined(motivo))
            throw new ArgumentOutOfRangeException(nameof(motivo));

        if (data.Date > hoje.Date)
            throw new InvalidOperationException("A data de encerramento não pode ser futura.");

        if (data.Date < DataInvestimento.Date)
            throw new InvalidOperationException("A data de encerramento não pode ser anterior à data do investimento.");

        EstaAtivo = false;
        MotivoEncerramento = motivo;
        DataEncerramento = data.Date;
    }

    public void Reativar()
    {
        if (EstaAtivo)
            return;

        EstaAtivo = true;
        MotivoEncerramento = null;
        DataEncerramento = null;
    }
}
