using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Service.Test.Arquitetura;

public class CancellationTokenTests
{
    // Lista de pendências de interfaces de Application e Persistence. Começou com as
    // pendências do início da Fatia 0, foi esvaziada pelas fatias e deve continuar vazia.
    // O teste falha se um item já estiver conforme.
    private static readonly HashSet<string> Pendencias =
    [
    ];

    // Exceção permanente (ADR 0017): o AuthenticationService do Identity usa
    // CancellationToken.None, então o token na ação não cancelaria nada. O teste falha se
    // uma delas passar a receber o token, para a exceção não ficar esquecida.
    private static readonly HashSet<string> AcoesDeControllerSemToken =
    [
        "AuthenticationController.Authenticate",
        "AuthenticationController.RegisterUser",
        "TokenController.Refresh",
    ];

    [Fact]
    public void AcoesDosControllersDevemReceberToken()
    {
        var acoes = typeof(Service.Auth.TokenController).Assembly
            .GetTypes()
            .Where(tipo => tipo.IsClass && !tipo.IsAbstract && typeof(ControllerBase).IsAssignableFrom(tipo))
            .SelectMany(tipo => tipo
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(metodo => metodo.GetCustomAttributes<HttpMethodAttribute>().Any() && RetornaTask(metodo))
                .Select(metodo => (Chave: $"{tipo.Name}.{metodo.Name}", Metodo: metodo)))
            .ToList();

        // Nas ações o token pode vir antes de parâmetros opcionais (page, pageSize): o C# não
        // aceita parâmetro obrigatório depois de opcional. Por isso a regra é "obrigatório, em
        // qualquer posição", e não "último".
        // Se a descoberta falhar (por exemplo, atributo renomeado), o teste não pode passar vazio.
        Assert.NotEmpty(acoes);

        var semToken = acoes
            .Where(item => !PossuiCancellationTokenObrigatorio(item.Metodo))
            .Select(item => item.Chave)
            .ToHashSet(StringComparer.Ordinal);

        var jaConformes = AcoesDeControllerSemToken.Except(semToken).ToArray();
        var novas = semToken.Except(AcoesDeControllerSemToken).ToArray();

        Assert.True(
            jaConformes.Length == 0,
            $"Remova da lista as ações que já recebem o CancellationToken: {string.Join(", ", jaConformes.Order())}");

        Assert.True(
            novas.Length == 0,
            $"Ações de controller sem parâmetro CancellationToken obrigatório: {string.Join(", ", novas.Order())}");
    }

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

    private static bool PossuiCancellationTokenObrigatorio(MethodInfo metodo)
    {
        return metodo.GetParameters()
            .Any(parametro => parametro.ParameterType == typeof(CancellationToken) && !parametro.IsOptional);
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
