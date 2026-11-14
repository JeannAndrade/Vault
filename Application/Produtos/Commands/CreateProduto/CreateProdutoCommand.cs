using LumiaFoundation.Core.Validators;
using Application.Repositories;

namespace Application.Produtos.Commands.CreateProduto;

public class CreateProdutoCommand(IRepositoryManager repositoryManager) : ICreateProdutoCommand
{
  private readonly IRepositoryManager _repositoryManager = repositoryManager;

    public async Task<ProdutoModel> ExecuteAsync(ProdutoModelForCreation produtoModel, Guid ownerId, CancellationToken cancellationToken)
  {
    CommandValidator.Validate(produtoModel);

    var produto = produtoModel.ToDomain(ownerId);
    _repositoryManager.Produto.Create(produto);
        await _repositoryManager.SaveAsync(cancellationToken);

    return ProdutoModel.FromDomain(produto);
  }
}
