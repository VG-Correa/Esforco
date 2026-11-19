using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Windows.Automation;
using Esforco.Infra;

namespace Esforco.Ui;

/// <summary>
/// Lê os nomes das abas de uma janela de navegador, do mesmo jeito que a barra de endereço é lida.
/// Só consulta nomes e estados: não altera a janela e não entra no conteúdo das páginas.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class LeitorDeAbasDoNavegador : ILeitorDeAbasDoNavegador
{
    private const int MaximoDeElementos = 600;
    private const int MaximoDeAbas = 300;
    private const int ProfundidadeMaxima = 10;
    private const string FaixaDoChromium = "TabContainerImpl";
    private static readonly TimeSpan Limite = TimeSpan.FromMilliseconds(1500);

    public AbasLidas? Ler(IntPtr janela)
    {
        if (janela == IntPtr.Zero)
            return null;

        try
        {
            var pedido = new CacheRequest { AutomationElementMode = AutomationElementMode.Full };
            pedido.Add(AutomationElement.ControlTypeProperty);
            pedido.Add(AutomationElement.ClassNameProperty);
            pedido.Add(AutomationElement.NameProperty);
            using (pedido.Activate())
                return Percorrer(AutomationElement.FromHandle(janela));
        }
        catch (Exception ex) when (ex is ElementNotAvailableException or InvalidOperationException or COMException or TimeoutException)
        {
            return null;
        }
    }

    private static AbasLidas? Percorrer(AutomationElement raiz)
    {
        var abas = new List<string>();
        var completa = true;
        var relogio = Stopwatch.StartNew();
        var fila = new Queue<(AutomationElement Elemento, int Profundidade)>();
        fila.Enqueue((raiz, 0));
        var visitados = 0;

        while (fila.Count > 0)
        {
            if (visitados >= MaximoDeElementos || abas.Count > MaximoDeAbas || relogio.Elapsed > Limite)
                return null;

            var (elemento, profundidade) = fila.Dequeue();
            visitados++;
            if (profundidade >= ProfundidadeMaxima)
                continue;

            var faixaConhecida = string.Equals(elemento.Cached.ClassName, FaixaDoChromium, StringComparison.Ordinal);
            foreach (AutomationElement filho in elemento.FindAll(TreeScope.Children, Condition.TrueCondition))
            {
                var tipo = filho.Cached.ControlType;
                if (tipo == ControlType.TabItem)
                {
                    var nome = (filho.Cached.Name ?? "").Trim();
                    abas.Add(nome.Length > 500 ? nome[..500] : nome);

                    // Fora da faixa de abas do Chromium não se sabe se a lista traz todas as abas.
                    completa &= faixaConhecida;
                    continue;
                }

                // O conteúdo da página fica de fora: só a moldura do navegador é percorrida.
                if (tipo == ControlType.Document
                    || (filho.Cached.ClassName ?? "").Contains("RenderWidget", StringComparison.OrdinalIgnoreCase))
                    continue;

                // As abas de um grupo recolhido podem não aparecer na lista.
                if (tipo == ControlType.Tab && EstaRecolhido(filho))
                    completa = false;

                fila.Enqueue((filho, profundidade + 1));
            }
        }

        return new AbasLidas(abas, completa);
    }

    private static bool EstaRecolhido(AutomationElement elemento)
    {
        if (!elemento.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out var padrao)
            || padrao is not ExpandCollapsePattern grupo)
            return false;

        var estado = grupo.Current.ExpandCollapseState;
        return estado is ExpandCollapseState.Collapsed or ExpandCollapseState.PartiallyExpanded;
    }
}
