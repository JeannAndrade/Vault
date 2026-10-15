using Application.Movimentos.Commands.UpdateValorLiquidoMovimento;
using Application.Movimentos.DistribuicaoValorLiquido;
using Domain.Movimentos.Distribuicao;
using LumiaFoundation.Core.Domain.Exceptions;
using LumiaFoundation.Core.TimeService;
using LumiaFoundation.Core.Validators;
using Persistence.Managment;
namespace Application.Movimentos.Commands.DistribuirValorLiquido;
public class DistribuirValorLiquidoCommand(IRepositoryManager repository, IDateTimeProvider clock) : IDistribuirValorLiquidoCommand
{
    public async Task ExecuteAsync(DistribuicaoValorLiquidoParameters parametros, Guid ownerId, CancellationToken cancellationToken)
    {
        CommandValidator.Validate(parametros);
        var hoje = clock.GetDateTime().Date;
        var movimentos = await repository.Movimento.GetAtivosDoTituloParaAtualizacaoAsync(ownerId, parametros.ToTituloFiltro(), cancellationToken);
        var resultado = DistribuicaoValorLiquidoCalculador.Calcular(DistribuicaoAuxiliar.Aportes(movimentos), parametros.ValorLiquidoTotal, hoje);
        if (!resultado.Sucesso)
            throw new CommandValidationException(DistribuicaoValorLiquidoModel.MensagemDoErro(resultado.Erro!.Value));
        var porId = DistribuicaoAuxiliar.PorId(movimentos);
        foreach (var aporte in resultado.Aportes!)
        {
            var movimento = porId[aporte.Id];
            new MovimentoModelForUpdateValorLiquido { ValorLiquidoAtual = aporte.ValorLiquido }.UpdateDomain(movimento, hoje);
            repository.Movimento.Update(movimento);
        }
        await repository.SaveAsync(cancellationToken);
    }
}
