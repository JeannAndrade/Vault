namespace Domain.Objetivos.Projecao;

public sealed record MovimentoParaProjecao(
    decimal ValorAporte,
    decimal ValorLiquidoAtual,
    DateTime DataInvestimento,
    DateTime DataAtualizacaoValorLiquido,
    bool EhReinvestimento,
    bool EstaAtivo,
    bool CompoeSaldo);
