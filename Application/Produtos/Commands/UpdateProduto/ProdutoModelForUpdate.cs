using System.ComponentModel.DataAnnotations;
using Domain.Produtos;

namespace Application.Produtos.Commands.UpdateProduto;

public record ProdutoModelForUpdate
{
  [Required(ErrorMessage = "Nome do Produto é obrigatório")]
  [MaxLength(60, ErrorMessage = "Nome do Produto não deve exceder 60 caracteres")]
  public string Nome { get; set; } = string.Empty;

  public Produto UpdateDomain(Produto produto)
  {
    produto.Nome = Nome;
    return produto;
  }
}
