namespace Esforco.Dominio;

public static class InterpretadorDeChatwoot
{
    public const string NomeDoPrograma = "Chatwoot";

    public static bool EhAba(string? titulo, string? endereco) =>
        ContemChatwoot(titulo) || EhEndereco(endereco);

    public static bool SaiuDaConversa(string? endereco)
    {
        if (!TryUri(endereco, out var uri))
            return false;

        if (!EhHost(uri.Host))
            return true;

        return !uri.AbsolutePath.Contains("/conversation/", StringComparison.OrdinalIgnoreCase);
    }

    public static string Titulo(IEnumerable<string?> nomes)
    {
        ArgumentNullException.ThrowIfNull(nomes);
        foreach (var nome in nomes)
        {
            var texto = NomesDePainel.Exibir(nome);
            if (EhTituloDoChat(texto))
                return texto;
        }

        return "";
    }

    private static bool EhTituloDoChat(string texto)
    {
        if (texto.Length is < 8 or > 80)
            return false;

        if (!texto.StartsWith("SplitC", StringComparison.OrdinalIgnoreCase))
            return false;

        var resto = texto["SplitC".Length..].TrimStart();
        if (resto.StartsWith("<>", StringComparison.Ordinal))
            resto = resto[2..];
        else if (resto.Length > 0 && resto[0] is '-' or '\u2013' or '\u2014')
            resto = resto[1..];
        else
            return false;

        return resto.Trim().Length > 0;
    }

    private static bool ContemChatwoot(string? titulo) =>
        (titulo ?? "").Contains("chatwoot", StringComparison.OrdinalIgnoreCase);

    private static bool EhEndereco(string? endereco) =>
        TryUri(endereco, out var uri) && EhHost(uri.Host);

    private static bool EhHost(string host) =>
        host.Equals("ticketing.splitc.com.br", StringComparison.OrdinalIgnoreCase)
        || host.Contains("chatwoot", StringComparison.OrdinalIgnoreCase);

    private static bool TryUri(string? endereco, out Uri uri)
    {
        uri = null!;
        var texto = (endereco ?? "").Trim();
        return texto.Length > 0 && Uri.TryCreate(texto, UriKind.Absolute, out uri!);
    }

}
