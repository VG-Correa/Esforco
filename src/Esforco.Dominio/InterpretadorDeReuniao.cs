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
        }

        return texto.Equals("Meet", StringComparison.OrdinalIgnoreCase)
            || texto.StartsWith("Meet:", StringComparison.OrdinalIgnoreCase)
            || texto.StartsWith("Meet -", StringComparison.OrdinalIgnoreCase);
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
