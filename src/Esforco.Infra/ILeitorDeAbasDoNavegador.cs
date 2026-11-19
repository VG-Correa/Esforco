namespace Esforco.Infra;

/// <summary>
/// Os nomes das abas de uma janela. Completa diz que a lista traz todas as abas: não é o caso
/// quando há grupo de abas recolhido ou quando a faixa de abas não é a de um navegador conhecido.
/// </summary>
public sealed record AbasLidas(IReadOnlyList<string> Nomes, bool Completa);

public interface ILeitorDeAbasDoNavegador
{
    /// <summary>
    /// As abas da janela, ou nulo quando a leitura não foi até o fim. Não altera a janela.
    /// </summary>
    AbasLidas? Ler(IntPtr janela);
}
