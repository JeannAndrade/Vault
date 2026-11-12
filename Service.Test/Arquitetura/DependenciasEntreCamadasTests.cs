using System.Xml.Linq;

namespace Service.Test.Arquitetura;

/// <summary>
/// Garante a direção das dependências (ADR 0023) lendo os .csproj de cada camada.
/// Lê o que está declarado, e não o que o compilador enxergou: assim também pega uma
/// referência ou pacote declarado e ainda não usado, que é como o vazamento começa.
/// </summary>
public class DependenciasEntreCamadasTests
{
    private const string ArquivoDaSolucao = "Vault.slnx";

    // Dependências apontam para dentro: Domain <- Application <- Persistence / Service.
    // Os projetos permitidos são uma lista fechada: qualquer ProjectReference fora dela falha.
    private sealed record Regra(
        string Projeto,
        string[] ProjetosPermitidos,
        string[]? PacotesPermitidos = null,
        string[]? PrefixosDePacotesProibidos = null);

    private static readonly Regra[] Regras =
    [
        new("Domain", ProjetosPermitidos: [], PacotesPermitidos: ["Lumia.Foundation.Core"]),
        new("Application", ProjetosPermitidos: ["Domain"], PacotesPermitidos: ["Lumia.Foundation.Core"]),
        new("Persistence", ProjetosPermitidos: ["Application", "Domain"],
            PrefixosDePacotesProibidos: ["Lumia.Foundation.AspNetCore", "Microsoft.AspNetCore"]),
        new("Service", ProjetosPermitidos: ["Application", "Domain", "Persistence"]),
        // O Web nunca acessa o banco nem a Application: só conversa com a Api por contrato (pacotes da Foundation).
        new("Presentation", ProjetosPermitidos: [],
            PrefixosDePacotesProibidos: ["Lumia.Foundation.EFRepository", "Microsoft.EntityFrameworkCore"]),
    ];

    public static TheoryData<string> Projetos => [.. Regras.Select(r => r.Projeto)];

    [Theory]
    [MemberData(nameof(Projetos))]
    public void ProjetosReferenciadosDevemEstarPermitidos(string projeto)
    {
        var regra = ObterRegra(projeto);

        var violacoes = LerProjetosReferenciados(projeto)
            .Where(referenciado => !regra.ProjetosPermitidos.Contains(referenciado))
            .ToList();

        Assert.True(violacoes.Count == 0,
            $"{projeto} não pode referenciar: {string.Join(", ", violacoes)}. Permitidos: [{string.Join(", ", regra.ProjetosPermitidos)}].");
    }

    [Theory]
    [MemberData(nameof(Projetos))]
    public void PacotesReferenciadosDevemRespeitarARegraDaCamada(string projeto)
    {
        var regra = ObterRegra(projeto);
        var pacotes = LerPacotesReferenciados(projeto);
        var violacoes = new List<string>();

        if (regra.PacotesPermitidos is not null)
        {
            violacoes.AddRange(pacotes.Where(pacote => !regra.PacotesPermitidos.Contains(pacote)));
        }

        if (regra.PrefixosDePacotesProibidos is not null)
        {
            violacoes.AddRange(pacotes.Where(pacote => regra.PrefixosDePacotesProibidos
                .Any(prefixo => pacote.StartsWith(prefixo, StringComparison.OrdinalIgnoreCase))));
        }

        Assert.True(violacoes.Count == 0,
            $"{projeto} referencia pacotes fora da regra da camada: {string.Join(", ", violacoes.Distinct())}.");
    }

    [Fact]
    public void TodoProjetoDeProducaoDeveTerRegra()
    {
        var comRegra = Regras.Select(r => r.Projeto).ToHashSet();

        var semRegra = Directory.EnumerateFiles(RaizDoRepositorio(), "*.csproj", SearchOption.AllDirectories)
            .Select(Path.GetFileNameWithoutExtension)
            .OfType<string>()
            .Where(nome => !nome.EndsWith(".Test", StringComparison.Ordinal) && !comRegra.Contains(nome))
            .ToList();

        Assert.True(semRegra.Count == 0,
            $"Projetos sem regra de dependência (inclua em {nameof(Regras)}): {string.Join(", ", semRegra)}.");
    }

    private static Regra ObterRegra(string projeto) => Regras.Single(r => r.Projeto == projeto);

    private static List<string> LerProjetosReferenciados(string projeto) =>
        [.. CarregarCsproj(projeto)
            .Descendants("ProjectReference")
            .Select(e => (string?)e.Attribute("Include"))
            .OfType<string>()
            // Os .csproj usam "\" nos caminhos; normaliza para funcionar também no Linux.
            .Select(caminho => Path.GetFileNameWithoutExtension(caminho.Replace('\\', '/')))];

    private static List<string> LerPacotesReferenciados(string projeto) =>
        [.. CarregarCsproj(projeto)
            .Descendants("PackageReference")
            .Select(e => (string?)e.Attribute("Include"))
            .OfType<string>()];

    private static XDocument CarregarCsproj(string projeto)
    {
        var caminho = Path.Combine(RaizDoRepositorio(), projeto, $"{projeto}.csproj");
        Assert.True(File.Exists(caminho), $"Arquivo do projeto não encontrado: {caminho}");
        return XDocument.Load(caminho);
    }

    // Sobe a partir da pasta de execução dos testes até achar a solução. Falha com mensagem
    // clara se os testes rodarem fora do repositório (por exemplo, sem o código-fonte).
    private static string RaizDoRepositorio()
    {
        var pasta = new DirectoryInfo(AppContext.BaseDirectory);
        while (pasta is not null && !File.Exists(Path.Combine(pasta.FullName, ArquivoDaSolucao)))
        {
            pasta = pasta.Parent;
        }

        return pasta?.FullName
            ?? throw new InvalidOperationException($"{ArquivoDaSolucao} não encontrado acima de {AppContext.BaseDirectory}.");
    }
}
