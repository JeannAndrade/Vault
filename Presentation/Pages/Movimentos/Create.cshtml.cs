using System.ComponentModel.DataAnnotations;
using LumiaFoundation.Core.ValidationAttributes;
using LumiaFoundation.Http.Client.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Presentation.Movimentos;

namespace Presentation.Pages.Movimentos;

public class CreateModel(IMovimentoApi movimentoApi, IMovimentoFormLookupData lookupData) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public MovimentoFormLookupData Lookup { get; private set; } = null!;

    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Lookup = await lookupData.LoadAsync(cancellationToken);
        Input.DataInvestimento = DateTime.Today;
        Input.EstaAtivo = true;
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        Lookup = await lookupData.LoadAsync(cancellationToken);

        if (!ModelState.IsValid)
            return Page();

        try
        {
            await movimentoApi.CreateAsync(Input.ToCreationDto(), cancellationToken);
        }
        catch (ApiException)
        {
            ErrorMessage = "Não foi possível salvar o movimento agora. Tente novamente em instantes.";
            return Page();
        }

        return RedirectToPage("Index");
    }

    public class InputModel
    {
        [NotEmptyGuid]
        [Display(Name = "Objetivo")]
        public Guid ObjetivoId { get; set; }

        [NotEmptyGuid]
        [Display(Name = "Tipo de Renda")]
        public Guid TipoRendaId { get; set; }

        [NotEmptyGuid]
        [Display(Name = "Corretora")]
        public Guid CorretoraId { get; set; }

        [NotEmptyGuid]
        [Display(Name = "Produto")]
        public Guid ProdutoId { get; set; }

        [NotEmptyGuid]
        [Display(Name = "Emissor")]
        public Guid EmissorId { get; set; }

        [StringLength(60, ErrorMessage = "A rentabilidade contratada não deve exceder 60 caracteres.")]
        public string? RentabilidadeContratada { get; set; }

        [StringLength(60, ErrorMessage = "A cotação na compra não deve exceder 60 caracteres.")]
        public string? CotacaoNaCompra { get; set; }

        [StringLength(200, ErrorMessage = "A observação não deve exceder 200 caracteres.")]
        public string? Observacao { get; set; }

        [StringLength(30, ErrorMessage = "O protocolo não deve exceder 30 caracteres.")]
        public string? Protocolo { get; set; }

        public decimal? Quantidade { get; set; }

        [Required(ErrorMessage = "Informe a data do investimento.")]
        [DataType(DataType.Date)]
        public DateTime DataInvestimento { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DataVencimento { get; set; }

        [Required(ErrorMessage = "Informe o valor do aporte.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "O valor do aporte deve ser maior que zero.")]
        public decimal ValorAporte { get; set; }

        public bool EhReinvestimento { get; set; }

        public bool EstaAtivo { get; set; }

        [Required(ErrorMessage = "Informe o valor líquido atual.")]
        [Range(0, double.MaxValue, ErrorMessage = "O valor líquido atual não pode ser negativo.")]
        public decimal ValorLiquidoAtual { get; set; }

        public MovimentoForCreationDto ToCreationDto() => new()
        {
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
            EstaAtivo = EstaAtivo,
            ValorLiquidoAtual = ValorLiquidoAtual
        };

        public MovimentoForUpdateDto ToUpdateDto() => new()
        {
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
            EstaAtivo = EstaAtivo,
            ValorLiquidoAtual = ValorLiquidoAtual
        };
    }
}
