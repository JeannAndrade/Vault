using System.ComponentModel.DataAnnotations;
using Domain.Movimentos;
using LumiaFoundation.Core.ValidationAttributes;

namespace Application.Movimentos.Commands.CreateMovimento;

public record MovimentoModelForCreation : IValidatableObject
{
    [NotEmptyGuid]
    public Guid ObjetivoId { get; set; }

    [NotEmptyGuid]
    public Guid TipoRendaId { get; set; }

    [NotEmptyGuid]
    public Guid CorretoraId { get; set; }

    [NotEmptyGuid]
    public Guid ProdutoId { get; set; }

    [NotEmptyGuid]
    public Guid EmissorId { get; set; }

    [MaxLength(60, ErrorMessage = "Rentabilidade contratada não deve exceder 60 caracteres")]
    public string? RentabilidadeContratada { get; set; }

    [MaxLength(60, ErrorMessage = "Cotação na compra não deve exceder 60 caracteres")]
    public string? CotacaoNaCompra { get; set; }

    [MaxLength(200, ErrorMessage = "Observação não deve exceder 200 caracteres")]
    public string? Observacao { get; set; }

    [MaxLength(30, ErrorMessage = "Protocolo não deve exceder 30 caracteres")]
    public string? Protocolo { get; set; }

    public decimal? Quantidade { get; set; }

    [Required(ErrorMessage = "Data do investimento é obrigatória")]
    public DateTime DataInvestimento { get; set; }

    public DateTime? DataVencimento { get; set; }

    [Required(ErrorMessage = "Valor do aporte é obrigatório")]
    [Range(0.01, double.MaxValue, ErrorMessage = "O valor do aporte deve ser maior que zero")]
    public decimal ValorAporte { get; set; }

    public bool EhReinvestimento { get; set; }

    [EnumDataType(typeof(MotivoEncerramento), ErrorMessage = "Motivo do encerramento inválido")]
    public MotivoEncerramento? MotivoEncerramento { get; set; }

    public DateTime? DataEncerramento { get; set; }

    public bool AtualizaPorCotacao { get; set; }

    [Required(ErrorMessage = "Valor líquido atual é obrigatório")]
    [Range(0, double.MaxValue, ErrorMessage = "O valor líquido atual não pode ser negativo")]
    public decimal ValorLiquidoAtual { get; set; }

    public Movimento ToDomain(Guid userId, DateTime dataAtualizacaoValorLiquido) => new()
    {
        UserId = userId,
        ObjetivoId = ObjetivoId,
        TipoRendaId = TipoRendaId,
        CorretoraId = CorretoraId,
        ProdutoId = ProdutoId,
        EmissorId = EmissorId,
        RentabilidadeContratada = RentabilidadeContratada,
        CotacaoNaCompra = CotacaoNaCompra,
        Observacao = Observacao,
        Protocolo = Protocolo,
        Quantidade = Quantidade,
        DataInvestimento = DataInvestimento,
        DataVencimento = DataVencimento,
        ValorAporte = ValorAporte,
        EhReinvestimento = EhReinvestimento,
        EstaAtivo = true,
        ValorLiquidoAtual = ValorLiquidoAtual,
        DataAtualizacaoValorLiquido = dataAtualizacaoValorLiquido.Date,
        AtualizaPorCotacao = AtualizaPorCotacao
    };

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (AtualizaPorCotacao && (Quantidade is null || Quantidade <= 0))
            yield return new ValidationResult(
                "Quantidade é obrigatória e deve ser maior que zero quando o valor varia por cotação",
                new[] { nameof(Quantidade) });

        var possuiMotivo = MotivoEncerramento.HasValue;
        var possuiData = DataEncerramento.HasValue;
        if (possuiMotivo != possuiData)
        {
            if (!possuiMotivo)
                yield return new ValidationResult("Informe o motivo do encerramento", new[] { nameof(MotivoEncerramento) });
            if (!possuiData)
                yield return new ValidationResult("Informe a data do encerramento", new[] { nameof(DataEncerramento) });
        }
    }
}
