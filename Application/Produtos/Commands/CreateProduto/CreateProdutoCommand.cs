using LumiaFoundation.Core.Validators;
using Persistence.Managment;

namespace Application.Produtos.Commands.CreateProduto;

public class CreateProdutoCommand(IRepositoryManager repositoryManager) : ICreateProdutoCommand
{
  private readonly IRepositoryManager _repositoryManager = repositoryManager;

  public async Task<ProdutoModel> ExecuteAsync(ProdutoModelForCreation produtoModel)
  {
    CommandValidator.Validate(produtoModel);

    var produto = produtoModel.ToDomain();
    _repositoryManager.Produto.Create(produto);
    await _repositoryManager.SaveAsync();

    return ProdutoModel.FromDomain(produto);
  }
}
