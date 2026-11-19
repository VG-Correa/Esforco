using System.Runtime.Versioning;
using System.Windows.Automation;
using Esforco.Dominio;
using Esforco.Infra;

namespace Esforco.Ui;

/// <summary>
/// Lê o nome do chat aberto no Chatwoot. Não altera a janela.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class LeitorDeConversaDoChatwoot : ILeitorDeConversaDoNavegador
{
    public string LerTitulo(IntPtr janela)
    {
        if (janela == IntPtr.Zero)
            return "";

        try
        {
            var raiz = AutomationElement.FromHandle(janela);
            var documento = Documento(raiz);
            if (documento is null)
                return "";

            var ancora = Ancora(documento) ?? Resolver(documento);
            if (ancora is null)
                return "";

            var atual = ancora;
            for (var nivel = 0; nivel < 6 && atual is not null; nivel++)
            {
                var titulo = InterpretadorDeChatwoot.Titulo(Nomes(atual, 4, 40));
                if (titulo.Length > 0)
                    return titulo;

                atual = Pai(atual);
            }

            return "";
        }
        catch (Exception ex) when (ex is ElementNotAvailableException or InvalidOperationException)
        {
            return "";
        }
    }

    private static AutomationElement? Documento(AutomationElement raiz)
    {
        var fila = new Queue<(AutomationElement Elemento, int Profundidade)>();
        fila.Enqueue((raiz, 0));
        var visitados = 0;
        var limite = Environment.TickCount64 + 200;

        while (fila.Count > 0 && visitados < 400 && Environment.TickCount64 < limite)
        {
            var (elemento, profundidade) = fila.Dequeue();
            visitados++;
            if (!ReferenceEquals(elemento, raiz) && EhDocumento(elemento))
                return elemento;

            if (profundidade >= 8)
                continue;

            foreach (var filho in Filhos(elemento))
                fila.Enqueue((filho, profundidade + 1));
        }

        return null;
    }

    private static AutomationElement? Ancora(AutomationElement documento)
    {
        var fila = new Queue<AutomationElement>();
        fila.Enqueue(documento);
        var visitados = 0;
        var limite = Environment.TickCount64 + 500;
        AutomationElement? qualquer = null;

        while (fila.Count > 0 && visitados < 2000 && Environment.TickCount64 < limite)
        {
            var elemento = fila.Dequeue();
            visitados++;
            if (!ReferenceEquals(elemento, documento) && EhNumeroDaConversa(elemento))
            {
                qualquer ??= elemento;
                if (PertoDoCanal(elemento))
                    return elemento;
            }

            foreach (var filho in Filhos(elemento))
                fila.Enqueue(filho);
        }

        return qualquer;
    }

    private static bool PertoDoCanal(AutomationElement botao)
    {
        var atual = botao;
        for (var nivel = 0; nivel < 3 && atual is not null; nivel++)
        {
            foreach (var nome in Nomes(atual, 2, 24))
            {
                if (nome.Contains("Whatsapp", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            atual = Pai(atual);
        }

        return false;
    }

    private static AutomationElement? Resolver(AutomationElement documento)
    {
        var fila = new Queue<AutomationElement>();
        fila.Enqueue(documento);
        var visitados = 0;
        var limite = Environment.TickCount64 + 200;

        while (fila.Count > 0 && visitados < 400 && Environment.TickCount64 < limite)
        {
            var elemento = fila.Dequeue();
            visitados++;
            if (!ReferenceEquals(elemento, documento) && EhResolver(elemento))
                return elemento;

            foreach (var filho in Filhos(elemento))
                fila.Enqueue(filho);
        }

        return null;
    }

    private static IEnumerable<string> Nomes(AutomationElement raiz, int profundidadeMaxima, int maximo)
    {
        var fila = new Queue<(AutomationElement Elemento, int Profundidade)>();
        fila.Enqueue((raiz, 0));
        var visitados = 0;
        while (fila.Count > 0 && visitados < maximo)
        {
            var (elemento, profundidade) = fila.Dequeue();
            visitados++;
            var nome = Nome(elemento);
            if (nome.Length > 0)
                yield return nome;

            if (profundidade >= profundidadeMaxima)
                continue;

            foreach (var filho in Filhos(elemento))
                fila.Enqueue((filho, profundidade + 1));
        }
    }

    private static bool EhNumeroDaConversa(AutomationElement elemento) =>
        NomeDoBotao(elemento) is string nome
        && nome.Length > 1
        && nome[0] == '#'
        && nome[1..].All(char.IsDigit);

    private static bool EhResolver(AutomationElement elemento)
    {
        var nome = NomeDoBotao(elemento);
        return nome is not null
            && (nome.Equals("Resolver", StringComparison.OrdinalIgnoreCase)
                || nome.Equals("Resolve", StringComparison.OrdinalIgnoreCase));
    }

    private static string? NomeDoBotao(AutomationElement elemento)
    {
        try
        {
            if (elemento.Current.ControlType != ControlType.Button)
                return null;

            var nome = (elemento.Current.Name ?? "").Trim();
            return nome.Length == 0 ? null : nome;
        }
        catch (ElementNotAvailableException)
        {
            return null;
        }
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
            return false;
        }
    }

    private static AutomationElement? Pai(AutomationElement elemento)
    {
        try
        {
            return TreeWalker.ControlViewWalker.GetParent(elemento);
        }
        catch (ElementNotAvailableException)
        {
            return null;
        }
    }

    private static IEnumerable<AutomationElement> Filhos(AutomationElement elemento)
    {
        AutomationElementCollection filhos;
        try
        {
            filhos = elemento.FindAll(TreeScope.Children, Condition.TrueCondition);
        }
        catch (ElementNotAvailableException)
        {
            yield break;
        }

        foreach (AutomationElement filho in filhos)
            yield return filho;
    }

    private static string Nome(AutomationElement elemento)
    {
        try
        {
            return elemento.Current.Name ?? "";
        }
        catch (ElementNotAvailableException)
        {
            return "";
        }
    }
}
