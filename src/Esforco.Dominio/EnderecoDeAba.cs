namespace Esforco.Dominio;

/// <summary>
/// Reduz o endereço da aba ao site e ao caminho. O que vem depois de ? ou # fica de fora,
/// porque pode trazer dados de sessão e muda sem a aba mudar.
/// </summary>
public static class EnderecoDeAba
{
    public static string Normalizar(string? endereco)
    {
        var texto = (endereco ?? "").Trim();
        if (texto.Length == 0)
            return "";

        if (!texto.Contains("://", StringComparison.Ordinal))
            texto = "https://" + texto;

        if (!Uri.TryCreate(texto, UriKind.Absolute, out var uri) || uri.Host.Length == 0)
            return "";

        var caminho = uri.AbsolutePath.TrimEnd('/');
        var chave = uri.Host.ToLowerInvariant() + caminho;
        return chave.Length > 300 ? chave[..300] : chave;
    }
}
