namespace Esforco.Dominio;

public static class ExtratorDeContexto
{
    public static string Extrair(string? titulo)
    {
        if (string.IsNullOrWhiteSpace(titulo))
            return "";

        var normalizado = titulo.Trim()
            .Replace(" — ", " - ", StringComparison.Ordinal)
            .Replace(" – ", " - ", StringComparison.Ordinal)
            .Replace(" • ", " - ", StringComparison.Ordinal);

        var partes = normalizado.Split(
            " - ",
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (partes.Length == 0)
            return "";

        var primeira = Limpar(partes[0]);
        if (primeira.Length == 0)
            return "";

        var barra = Math.Max(primeira.LastIndexOf('\\'), primeira.LastIndexOf('/'));
        if (barra >= 0 && barra < primeira.Length - 1)
            primeira = Limpar(primeira[(barra + 1)..]);

        return primeira;
    }

    private static string Limpar(string texto) => texto.Trim().Trim('*').Trim();
}
