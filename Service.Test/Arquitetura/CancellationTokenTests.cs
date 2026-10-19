using System.Reflection;

namespace Service.Test.Arquitetura;

public class CancellationTokenTests
{
    // Esta lista representa as pendências conhecidas no início da Fatia 0.
    // Cada fatia deve removê-las; o teste falha se um item já estiver conforme.
    private static readonly HashSet<string> Pendencias =
    [
        "ICorretoraRepository.GetAllAsync",
        "ICorretoraRepository.GetAsync",
        "ICreateCorretoraCommand.ExecuteAsync",
        "ICreateEmissorCommand.ExecuteAsync",
        "ICreateMovimentoCommand.ExecuteAsync",
        "ICreateObjetivoCommand.ExecuteAsync",
        "ICreateProdutoCommand.ExecuteAsync",
        "ICreateTipoRendaCommand.ExecuteAsync",
        "IDeleteCorretoraCommand.ExecuteAsync",
        "IDeleteEmissorCommand.ExecuteAsync",
        "IDeleteMovimentoCommand.ExecuteAsync",
        "IDeleteObjetivoCommand.ExecuteAsync",
        "IDeleteProdutoCommand.ExecuteAsync",
        "IDeleteTipoRendaCommand.ExecuteAsync",
        "IEmissorRepository.GetAllAsync",
        "IEmissorRepository.GetAsync",
        "IGetCorretoraQuery.ExecuteAsync",
        "IGetCorretorasListQuery.ExecuteAsync",
        "IGetEmissoresListQuery.ExecuteAsync",
        "IGetEmissorQuery.ExecuteAsync",
        "IGetMovimentoQuery.ExecuteAsync",
        "IGetMovimentosListQuery.ExecuteAsync",
        "IGetObjetivoQuery.ExecuteAsync",
        "IGetObjetivosListQuery.ExecuteAsync",
        "IGetProdutoQuery.ExecuteAsync",
        "IGetProdutosListQuery.ExecuteAsync",
        "IGetProximosVencimentosQuery.ExecuteAsync",
        "IGetResumoObjetivosListQuery.ExecuteAsync",
        "IGetTipoRendaQuery.ExecuteAsync",
        "IGetTiposRendaListQuery.ExecuteAsync",
        "IMovimentoReferenciasValidator.ValidarAsync",
        "IMovimentoRepository.ExistemPorCorretoraAsync",
        "IMovimentoRepository.ExistemPorEmissorAsync",
        "IMovimentoRepository.ExistemPorObjetivoAsync",
        "IMovimentoRepository.ExistemPorProdutoAsync",
        "IMovimentoRepository.ExistemPorTipoRendaAsync",
        "IMovimentoRepository.GetAllAsync",
        "IMovimentoRepository.GetAsync",
        "IMovimentoRepository.GetPagedWithRelatedEntitiesAsync",
        "IMovimentoRepository.GetProximosVencimentosAsync",
        "IMovimentoRepository.GetWithRelatedEntitiesAsync",
        "IObjetivoRepository.GetAllAsync",
        "IObjetivoRepository.GetAllWithRelatedEntitiesAsync",
        "IObjetivoRepository.GetAsync",
        "IProdutoRepository.GetAllAsync",
        "IProdutoRepository.GetAsync",
        "ITipoRendaRepository.GetAllAsync",
        "ITipoRendaRepository.GetAsync",
        "IUpdateCorretoraCommand.ExecuteAsync",
        "IUpdateEmissorCommand.ExecuteAsync",
        "IUpdateMovimentoCommand.ExecuteAsync",
        "IUpdateObjetivoCommand.ExecuteAsync",
        "IUpdateProdutoCommand.ExecuteAsync",
        "IUpdateTipoRendaCommand.ExecuteAsync"
    ];

    [Fact]
    public void MetodosDeIQueryICommandIRepositoryEValidatorDevemReceberToken()
    {
        var metodos = ObterMetodosAlvo()
            .Select(item => new
            {
                item.Chave,
                Conforme = PossuiCancellationTokenObrigatorioComoUltimoParametro(item.Metodo)
            })
            .ToList();

        var pendenciasEncontradas = metodos
            .Where(item => !item.Conforme)
            .Select(item => item.Chave)
            .ToHashSet(StringComparer.Ordinal);

        var pendenciasInexistentes = Pendencias.Except(pendenciasEncontradas).ToArray();
        var novasPendencias = pendenciasEncontradas.Except(Pendencias).ToArray();

        Assert.True(
            pendenciasInexistentes.Length == 0,
            $"Remova da allowlist as pendências que já estão conformes: {string.Join(", ", pendenciasInexistentes.Order())}");

        Assert.True(
            novasPendencias.Length == 0,
            $"Métodos assíncronos sem CancellationToken na allowlist: {string.Join(", ", novasPendencias.Order())}");
    }

    private static IEnumerable<(string Chave, MethodInfo Metodo)> ObterMetodosAlvo()
    {
        var assemblies = new[]
        {
            typeof(Application.Movimentos.Queries.GetMovimentoList.IGetMovimentosListQuery).Assembly,
            typeof(Persistence.Corretoras.ICorretoraRepository).Assembly
        };

        return assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(EhInterfaceAlvo)
            .SelectMany(tipo => tipo.GetMethods().Select(metodo =>
                (Chave: $"{tipo.Name}.{metodo.Name}", Metodo: metodo)))
            .Where(item => RetornaTask(item.Metodo));
    }

    private static bool EhInterfaceAlvo(Type tipo)
    {
        return tipo.IsInterface
            && tipo.Name.StartsWith("I", StringComparison.Ordinal)
            && (tipo.Name.EndsWith("Query", StringComparison.Ordinal)
                || tipo.Name.EndsWith("Command", StringComparison.Ordinal)
                || tipo.Name.EndsWith("Repository", StringComparison.Ordinal)
                || tipo == typeof(Application.Movimentos.Validation.IMovimentoReferenciasValidator));
    }

    private static bool RetornaTask(MethodInfo metodo)
    {
        return typeof(Task).IsAssignableFrom(metodo.ReturnType);
    }

    private static bool PossuiCancellationTokenObrigatorioComoUltimoParametro(MethodInfo metodo)
    {
        var parametros = metodo.GetParameters();
        var ultimo = parametros.LastOrDefault();

        return ultimo is not null
            && ultimo.ParameterType == typeof(CancellationToken)
            && !ultimo.IsOptional;
    }
}
