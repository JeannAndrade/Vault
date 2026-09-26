using Application.Corretoras.Queries.GetCorretora;
using Application.Emissores.Queries.GetEmissor;
using Application.Objetivos.Queries.GetObjetivo;
using Application.Produtos.Queries.GetProduto;
using Application.TiposRenda.Queries.GetTipoRenda;

namespace Application.Movimentos.Validation;

public class MovimentoReferenciasValidator(
    IGetObjetivoQuery getObjetivoQuery,
    IGetTipoRendaQuery getTipoRendaQuery,
    IGetCorretoraQuery getCorretoraQuery,
    IGetProdutoQuery getProdutoQuery,
    IGetEmissorQuery getEmissorQuery) : IMovimentoReferenciasValidator
{
    private readonly IGetObjetivoQuery _getObjetivoQuery = getObjetivoQuery;
    private readonly IGetTipoRendaQuery _getTipoRendaQuery = getTipoRendaQuery;
    private readonly IGetCorretoraQuery _getCorretoraQuery = getCorretoraQuery;
    private readonly IGetProdutoQuery _getProdutoQuery = getProdutoQuery;
    private readonly IGetEmissorQuery _getEmissorQuery = getEmissorQuery;

    public async Task ValidarAsync(
        Guid ownerId,
        Guid objetivoId,
        Guid tipoRendaId,
        Guid corretoraId,
        Guid produtoId,
        Guid emissorId)
    {
        // Sequencial (sem Task.WhenAll): cada Query já valida existência + posse
        // (ownerId) da entidade, e queremos que a primeira referência inválida
        // interrompa a validação com uma mensagem clara sobre qual entidade falhou.
        await _getObjetivoQuery.ExecuteAsync(ownerId, objetivoId);
        await _getTipoRendaQuery.ExecuteAsync(ownerId, tipoRendaId);
        await _getCorretoraQuery.ExecuteAsync(ownerId, corretoraId);
        await _getProdutoQuery.ExecuteAsync(ownerId, produtoId);
        await _getEmissorQuery.ExecuteAsync(ownerId, emissorId);
    }
}
