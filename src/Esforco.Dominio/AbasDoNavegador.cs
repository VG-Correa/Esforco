namespace Esforco.Dominio;

public enum PresencaDeAba
{
    /// <summary>Uma aba com o título está aberta em alguma janela.</summary>
    Aberta = 0,

    /// <summary>Todas as janelas tiveram as abas lidas e nenhuma tem o título.</summary>
    Fechada = 1,

    /// <summary>Não deu para afirmar: alguma janela não teve as abas lidas, ou o título só aparece dentro de outro.</summary>
    Incerta = 2,
}

/// <summary>
/// O título da janela do navegador é o da aba que está à mostra. A aba atrás de outra só aparece
/// na lista de abas, e a aba fechada não aparece em lugar nenhum.
/// </summary>
public static class AbasDoNavegador
{
    /// <summary>
    /// Quanto tempo a aba pode ficar fora da lista sem ser dada como fechada. Cobre a página
    /// que recarrega e a aba arrastada de uma janela para outra.
    /// </summary>
    public static readonly TimeSpan Tolerancia = TimeSpan.FromSeconds(30);

    /// <summary>
    /// De quanto em quanto tempo as abas são lidas enquanto algo conta sem aparecer no título de uma janela.
    /// </summary>
    public static readonly TimeSpan Intervalo = TimeSpan.FromSeconds(5);

    private static readonly string[] Navegadores =
    [
        "Google Chrome",
        "Microsoft Edge",
        "Mozilla Firefox",
        "Brave",
        "Opera",
        "Chromium",
        "Vivaldi",
    ];

    public static PresencaDeAba Procurar(string? titulo, IEnumerable<JanelaAberta> janelas)
    {
        ArgumentNullException.ThrowIfNull(janelas);
        var pagina = Pagina(titulo).Texto;
        if (pagina.Length == 0)
            return PresencaDeAba.Incerta;

        var certeza = true;
        foreach (var janela in janelas)
        {
            if (janela.Abas is null)
            {
                certeza = false;
                continue;
            }

            foreach (var aba in janela.Abas)
            {
                var achou = Comparar(aba, pagina);
                if (achou == Achado.Inteiro)
                    return PresencaDeAba.Aberta;
                if (achou == Achado.Dentro)
                    certeza = false;
            }

            if (!Confiavel(janela))
                certeza = false;
        }

        return certeza ? PresencaDeAba.Fechada : PresencaDeAba.Incerta;
    }

    /// <summary>
    /// A lista de abas só vale como completa quando a aba à mostra, a do título da janela, está nela.
    /// Janela sem nome de navegador no título e sem aba nenhuma é janela de aplicativo: não esconde nada.
    /// </summary>
    public static bool Confiavel(JanelaAberta janela)
    {
        ArgumentNullException.ThrowIfNull(janela);
        if (janela.Abas is null || janela.PodeFaltarAba)
            return false;

        var (pagina, temNavegador) = Pagina(janela.Titulo);
        if (janela.Abas.Count == 0)
            return !temNavegador;

        return pagina.Length > 0 && janela.Abas.Any(aba => Comparar(aba, pagina) == Achado.Inteiro);
    }

    /// <summary>
    /// O título da página, sem o nome do navegador que a janela põe no fim.
    /// </summary>
    public static (string Texto, bool TemNavegador) Pagina(string? titulo)
    {
        var texto = Limpar(titulo);
        foreach (var navegador in Navegadores)
        {
            var cauda = " - " + navegador;
            if (texto.EndsWith(cauda, StringComparison.OrdinalIgnoreCase))
                return (texto[..^cauda.Length].Trim(), true);
        }

        return (texto, false);
    }

    private enum Achado
    {
        Nada = 0,
        Dentro = 1,
        Inteiro = 2,
    }

    /// <summary>
    /// O nome da aba traz o título e, em volta, o que o navegador acrescenta: o grupo, o uso de memória,
    /// o som tocando. O título vale como inteiro quando não é só o pedaço de um título maior.
    /// </summary>
    private static Achado Comparar(string? aba, string pagina)
    {
        var texto = Limpar(aba);
        var achado = Achado.Nada;
        var inicio = 0;
        while (inicio <= texto.Length - pagina.Length)
        {
            var achou = texto.IndexOf(pagina, inicio, StringComparison.OrdinalIgnoreCase);
            if (achou < 0)
                break;

            if (ComecaLimpo(texto, achou) && TerminaLimpo(texto, achou + pagina.Length))
                return Achado.Inteiro;

            achado = Achado.Dentro;
            inicio = achou + 1;
        }

        return achado;
    }

    private static bool ComecaLimpo(string texto, int inicio) =>
        inicio == 0
        || (inicio >= 2 && texto[inicio - 1] == ' ' && char.IsLetterOrDigit(texto[inicio - 2]));

    private static bool TerminaLimpo(string texto, int fim)
    {
        if (fim == texto.Length || texto[fim] == ':')
            return true;

        return texto[fim] == ' ' && fim + 1 < texto.Length && texto[fim + 1] == '-';
    }

    private static string Limpar(string? texto) =>
        (texto ?? "")
            .Replace("\u200B", "", StringComparison.Ordinal)
            .Replace(" — ", " - ", StringComparison.Ordinal)
            .Replace(" – ", " - ", StringComparison.Ordinal)
            .Trim();
}
