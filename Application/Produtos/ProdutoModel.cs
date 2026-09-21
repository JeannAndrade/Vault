using Domain.Produtos;

namespace Application.Produtos;

public record ProdutoModel
{
  public Guid Id { get; set; }
  public string Nome { get; set; } = string.Empty;
  public Guid UserId { get; set; }

  public static ProdutoModel FromDomain(Produto produto)
  {
    return new ProdutoModel
    {
      Id = produto.Id,
      Nome = produto.Nome,
      UserId = produto.UserId
    };
  }
}
