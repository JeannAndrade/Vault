using Application.Produtos;

namespace Service.Produtos.DTOs;

public class ProdutoDto
{
  public Guid Id { get; set; }
  public string Nome { get; set; } = string.Empty;

  public static ProdutoDto FromApplication(ProdutoModel produto)
  {
    return new ProdutoDto
    {
      Id = produto.Id,
      Nome = produto.Nome
    };
  }

  public static List<ProdutoDto> FromApplication(List<ProdutoModel> produtos)
  {
    return [.. produtos.Select(FromApplication)];
  }
}
