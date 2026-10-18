using Application.Movimentos.AtualizacaoPorCotacao;
using LumiaFoundation.Core.Domain.Exceptions;
using LumiaFoundation.Core.TimeService;
using LumiaFoundation.Core.Validators;
using Persistence.Managment;

namespace Application.Movimentos.Commands.AtualizarPorCotacao;

public class AtualizarPorCotacaoCommand(
    IRepositoryManager repositoryManager,
    IDateTimeProvider dateTimeProvider) : IAtualizarPorCotacaoCommand
{
    private readonly IRepositoryManager _repository = repositoryManager;
    private readonly IDateTimeProvider _dateTimeProvider = dateTimeProvider;

    public async Task<AtualizacaoPorCotacaoResultadoModel> ExecuteAsync(
        AtualizacaoPorCotacaoParameters parameters,
        Guid ownerId,
        CancellationToken cancellationToken)
    {
        CommandValidator.Validate(parameters);

        var papel = parameters.Papel!.Trim();
        var hoje = _dateTimeProvider.GetDateTime().Date;

        if (parameters.Data.Date > hoje)
            throw new CommandValidationException("A data da cotação não pode ser futura.");

        var cotacao = await _repository.Cotacao.GetPorPapelEDataAsync(
            ownerId,
            papel,
            parameters.Data.Date,
            trackChanges: true,
            cancellationToken);

        if (cotacao is null)
        {
            cotacao = new Domain.Cotacoes.Cotacao
            {
                UserId = ownerId,
                Papel = papel,
                Valor = parameters.Valor,
                Data = parameters.Data.Date
            };

            _repository.Cotacao.Create(cotacao);
        }
        else
        {
            cotacao.Valor = parameters.Valor;
        }

        var dataMaisRecente = await _repository.Cotacao.GetDataMaisRecenteAsync(ownerId, papel, cancellationToken);
        var deveRecalcular = dataMaisRecente is null || parameters.Data.Date >= dataMaisRecente.Value.Date;

        var quantidadeMovimentosAtualizados = 0;

        if (deveRecalcular)
        {
            var movimentos = await _repository.Movimento.GetAtivosPorCotacaoDoPapelAsync(ownerId, papel, cancellationToken);

            foreach (var movimento in movimentos)
            {
                movimento.AplicarCotacao(parameters.Valor, hoje);
                _repository.Movimento.Update(movimento);
            }

            quantidadeMovimentosAtualizados = movimentos.Count;
        }

        await _repository.SaveAsync(cancellationToken);

        return new AtualizacaoPorCotacaoResultadoModel(deveRecalcular, quantidadeMovimentosAtualizados);
    }
}
