namespace Esforco.Dominio;

public sealed record ReuniaoLida(string Nome, string Tema);

public static class InterpretadorDeReuniao
{
    private static readonly string[] Sufixos =
    [
        "google chrome",
        "microsoft edge",
        "mozilla firefox",
        "brave",
        "opera",
        "google meet",
        "meet",
    ];

    private static readonly string[] Prefixos =
    [
        "google meet: ",
        "google meet:",
        "google meet - ",
        "meet: ",
        "meet:",
        "meet - ",
    ];

    public static ReuniaoLida? Ler(string? titulo)
    {
        var texto = Limpar(titulo);
        var marca = texto.IndexOf("<>", StringComparison.Ordinal);
        if (marca <= 0)
            return null;

        var nome = NomesDePainel.Exibir(texto[..marca]);
        var resto = texto[(marca + 2)..].TrimStart();
        if (nome.Length == 0 || !ComeçaComSplitC(resto))
            return null;

        var depois = resto["SplitC".Length..].TrimStart();
        var tema = TemaDe(depois);
        return new ReuniaoLida(nome, tema);
    }

    public static bool PareceJanelaDoMeet(string? titulo)
    {
        if (Ler(titulo) is not null)
            return true;
        if (string.IsNullOrWhiteSpace(titulo))
            return false;

        var texto = SemNavegador(titulo);
        return texto.Equals("Meet", StringComparison.OrdinalIgnoreCase)
            || texto.StartsWith("Meet:", StringComparison.OrdinalIgnoreCase)
            || texto.StartsWith("Meet -", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// A tela do Meet antes de mostrar o nome da reunião: "Meet" ou "Meet: código".
    /// </summary>
    public static bool EhTelaDoMeetSemNome(string? titulo) =>
        Ler(titulo) is null && PareceJanelaDoMeet(titulo);

    /// <summary>
    /// O título tem nome de reunião e vem de um programa de chamada. Uma página que só cita a reunião,
    /// como o relatório dela, tem o nome mas não é a chamada.
    /// </summary>
    public static bool EhChamada(string? titulo)
    {
        if (Ler(titulo) is null)
            return false;

        var texto = Padronizar(titulo!);
        if (Prefixos.Any(prefixo => texto.StartsWith(prefixo, StringComparison.OrdinalIgnoreCase)))
            return true;

        return MarcasDeChamada.Any(marca => TemTrecho(texto, marca));
    }

    private static readonly string[] MarcasDeChamada = ["Google Meet", "Meet", "Microsoft Teams", "Zoom"];

    /// <summary>
    /// O nome do programa só vale quando é um trecho inteiro do título, separado por " - " ou " | ".
    /// </summary>
    private static bool TemTrecho(string texto, string trecho)
    {
        var inicio = 0;
        while (inicio < texto.Length)
        {
            var achou = texto.IndexOf(trecho, inicio, StringComparison.OrdinalIgnoreCase);
            if (achou < 0)
                return false;

            var fim = achou + trecho.Length;
            var antes = texto[..achou];
            var depois = texto[fim..];
            var abre = achou == 0 || antes.EndsWith(" - ", StringComparison.Ordinal) || antes.EndsWith(" | ", StringComparison.Ordinal);
            var fecha = depois.Length == 0 || depois.StartsWith(" - ", StringComparison.Ordinal) || depois.StartsWith(" | ", StringComparison.Ordinal);
            if (abre && fecha)
                return true;

            inicio = fim;
        }

        return false;
    }

    private static string Padronizar(string titulo) =>
        titulo.Trim()
            .Replace(" — ", " - ", StringComparison.Ordinal)
            .Replace(" – ", " - ", StringComparison.Ordinal)
            .Replace(" • ", " - ", StringComparison.Ordinal);

    private static string SemNavegador(string titulo)
    {
        var texto = Padronizar(titulo);
        var mudou = true;
        while (mudou)
        {
            mudou = false;
            foreach (var sufixo in Sufixos)
            {
                var cauda = " - " + sufixo;
                if (!texto.EndsWith(cauda, StringComparison.OrdinalIgnoreCase))
                    continue;

                texto = texto[..^cauda.Length].Trim();
                mudou = true;
            }
        }

        return texto;
    }

    public static bool Mesma(string? titulo, string? outro)
    {
        var uma = Ler(titulo);
        var outra = Ler(outro);
        if (uma is null || outra is null)
            return false;

        return NomesDePainel.Normalizar(uma.Nome) == NomesDePainel.Normalizar(outra.Nome)
            && NomesDePainel.Normalizar(uma.Tema) == NomesDePainel.Normalizar(outra.Tema);
    }

    private static string TemaDe(string depois)
    {
        if (depois.StartsWith('-'))
            depois = depois[1..];

        var tema = NomesDePainel.Exibir(depois);
        return tema.Length == 0 ? "Sem tema" : tema;
    }

    private static bool ComeçaComSplitC(string texto) =>
        texto.StartsWith("SplitC", StringComparison.OrdinalIgnoreCase)
        && (texto.Length == "SplitC".Length || !char.IsLetterOrDigit(texto["SplitC".Length]));

    private static string Limpar(string? titulo)
    {
        if (string.IsNullOrWhiteSpace(titulo))
            return "";

        var texto = titulo.Trim()
            .Replace(" — ", " - ", StringComparison.Ordinal)
            .Replace(" – ", " - ", StringComparison.Ordinal)
            .Replace(" • ", " - ", StringComparison.Ordinal);

        var mudou = true;
        while (mudou)
        {
            mudou = false;
            foreach (var sufixo in Sufixos)
            {
                var cauda = " - " + sufixo;
                if (!texto.EndsWith(cauda, StringComparison.OrdinalIgnoreCase))
                    continue;

                texto = texto[..^cauda.Length].Trim();
                mudou = true;
            }

            foreach (var prefixo in Prefixos)
            {
                if (!texto.StartsWith(prefixo, StringComparison.OrdinalIgnoreCase))
                    continue;

                texto = texto[prefixo.Length..].Trim();
                mudou = true;
            }
        }

        return texto;
    }
}
