using System.ComponentModel.DataAnnotations;
using LumiaFoundation.Http.Client.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Presentation.Objetivos;

namespace Presentation.Pages.Objetivos;

public class EditModel(IObjetivoApi objetivoApi) : PageModel
{
  [BindProperty(SupportsGet = true)]
  public Guid Id { get; set; }

  [BindProperty]
  public InputModel Input { get; set; } = new();

  public string? ErrorMessage { get; private set; }

  public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
  {
    var objetivo = await objetivoApi.GetByIdAsync(Id, cancellationToken);
    Input = new InputModel
    {
      Nome = objetivo.Nome,
      Descricao = objetivo.Descricao,
      Meta = objetivo.Meta,
      FontePagadora = objetivo.FontePagadora,
      AporteMensal = objetivo.AporteMensal,
      OndeAplicar = objetivo.OndeAplicar,
      EstaAtivo = objetivo.EstaAtivo
    };

    return Page();
  }

  public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
  {
    if (!ModelState.IsValid)
      return Page();

    try
    {
      await objetivoApi.UpdateAsync(Id, new ObjetivoForUpdateDto(
          Input.Nome.Trim(),
          string.IsNullOrWhiteSpace(Input.Descricao) ? null : Input.Descricao.Trim(),
          Input.Meta,
          Input.FontePagadora.Trim(),
          Input.AporteMensal,
          Input.OndeAplicar.Trim(),
          Input.EstaAtivo), cancellationToken);
    }
    catch (ApiException)
    {
      ErrorMessage = "Não foi possível salvar o objetivo agora. Tente novamente em instantes.";
      return Page();
    }

    return RedirectToPage("Index");
  }

  public class InputModel
  {
    [Required(ErrorMessage = "Informe o nome.")]
    [StringLength(100, ErrorMessage = "O nome não deve exceder 100 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "A descrição não deve exceder 500 caracteres.")]
    public string? Descricao { get; set; }

    [Range(typeof(decimal), "1", "79228162514264337593543950335", ErrorMessage = "A meta deve ser maior que zero.")]
    public decimal Meta { get; set; }

    [Required(ErrorMessage = "Informe a fonte pagadora.")]
    [StringLength(100, ErrorMessage = "A fonte pagadora não deve exceder 100 caracteres.")]
    public string FontePagadora { get; set; } = string.Empty;

    [Range(typeof(decimal), "1", "79228162514264337593543950335", ErrorMessage = "O aporte mensal deve ser maior que zero.")]
    public decimal AporteMensal { get; set; }

    [Required(ErrorMessage = "Informe onde aplicar.")]
    [StringLength(100, ErrorMessage = "Onde aplicar não deve exceder 100 caracteres.")]
    public string OndeAplicar { get; set; } = string.Empty;

    public bool EstaAtivo { get; set; }
  }
}
