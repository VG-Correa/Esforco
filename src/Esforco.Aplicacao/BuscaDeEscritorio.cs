using System.Globalization;

namespace Esforco.Aplicacao;

/// <summary>
/// A busca da lista de escritórios da janelinha de vínculo e o que fica marcado depois dela.
/// </summary>
public static class BuscaDeEscritorio
{
    private static readonly CompareInfo Comparador = CultureInfo.GetCultureInfo("pt-BR").CompareInfo;

    public static IReadOnlyList<T> Filtrar<T>(IReadOnlyList<T> itens, string? texto, Func<T, string> rotulo)
    {
        ArgumentNullException.ThrowIfNull(itens);
        ArgumentNullException.ThrowIfNull(rotulo);
        var procurado = (texto ?? "").Trim();
        if (procurado.Length == 0)
            return itens;

        return itens
            .Where(item => Comparador.IndexOf(
                rotulo(item),
                procurado,
                CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0)
            .ToList();
    }

    /// <summary>
    /// O escritório escolhido pela pessoa, ou vindo do vínculo salvo, continua marcado enquanto
    /// aparecer na lista, e volta a ficar marcado quando a busca deixa de escondê-lo.
    /// Só quando a busca o esconde é que o primeiro achado fica marcado no lugar.
    /// </summary>
    public static string? Marcar(string? escolhido, string? marcado, IReadOnlyList<string> achados, bool comBusca)
    {
        ArgumentNullException.ThrowIfNull(achados);
        if (!string.IsNullOrEmpty(escolhido) && achados.Contains(escolhido))
            return escolhido;
        if (!string.IsNullOrEmpty(marcado) && achados.Contains(marcado))
            return marcado;
        return comBusca && achados.Count > 0 ? achados[0] : null;
    }
}
