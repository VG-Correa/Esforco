using System.Runtime.Versioning;
using System.Windows.Automation;
using Esforco.Infra;

namespace Esforco.Ui;

/// <summary>
/// Lê o valor da barra de endereço. Não altera a janela.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class LeitorDeEnderecoDaBarra : ILeitorDeEnderecoDoNavegador
{
    public string Ler(IntPtr janela)
    {
        if (janela == IntPtr.Zero)
            return "";

        try
        {
            var barra = Procurar(AutomationElement.FromHandle(janela));
            if (barra is null)
                return "";

            if (!barra.TryGetCurrentPattern(ValuePattern.Pattern, out var padrao) || padrao is not ValuePattern valor)
                return "";

            var texto = valor.Current.Value?.Trim() ?? "";
            return PareceEndereco(texto) ? texto : "";
        }
        catch (Exception ex) when (ex is ElementNotAvailableException or InvalidOperationException)
        {
            return "";
        }
    }

    private static AutomationElement? Procurar(AutomationElement raiz)
    {
        var fila = new Queue<(AutomationElement Elemento, int Profundidade)>();
        fila.Enqueue((raiz, 0));
        var visitados = 0;

        while (fila.Count > 0 && visitados < 400)
        {
            var (elemento, profundidade) = fila.Dequeue();
            visitados++;

            if (!ReferenceEquals(elemento, raiz) && EhBarra(elemento))
                return elemento;

            if (profundidade >= 8 || (!ReferenceEquals(elemento, raiz) && EhDocumento(elemento)))
                continue;

            AutomationElementCollection filhos;
            try
            {
                filhos = elemento.FindAll(TreeScope.Children, Condition.TrueCondition);
            }
            catch (ElementNotAvailableException)
            {
                continue;
            }

            foreach (AutomationElement filho in filhos)
                fila.Enqueue((filho, profundidade + 1));
        }

        return null;
    }

    private static bool EhBarra(AutomationElement elemento)
    {
        string classe;
        string id;
        string nome;
        ControlType tipo;
        try
        {
            classe = elemento.Current.ClassName ?? "";
            id = elemento.Current.AutomationId ?? "";
            nome = elemento.Current.Name ?? "";
            tipo = elemento.Current.ControlType;
        }
        catch (ElementNotAvailableException)
        {
            return false;
        }

        if (classe.Contains("Omnibox", StringComparison.OrdinalIgnoreCase))
            return true;

        if (id.Equals("urlbar-input", StringComparison.OrdinalIgnoreCase))
            return true;

        return tipo == ControlType.Edit
            && (nome.Contains("endereço", StringComparison.OrdinalIgnoreCase)
                || nome.Contains("address", StringComparison.OrdinalIgnoreCase));
    }

    private static bool EhDocumento(AutomationElement elemento)
    {
        try
        {
            if (elemento.Current.ControlType == ControlType.Document)
                return true;

            var classe = elemento.Current.ClassName ?? "";
            return classe.Contains("RenderWidget", StringComparison.OrdinalIgnoreCase);
        }
        catch (ElementNotAvailableException)
        {
            return true;
        }
    }

    private static bool PareceEndereco(string texto) =>
        texto.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
        || texto.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
        || texto.Contains("://", StringComparison.Ordinal)
        || texto.Contains("app.splitc.com.br", StringComparison.OrdinalIgnoreCase);
}
