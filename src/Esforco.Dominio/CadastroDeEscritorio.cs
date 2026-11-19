namespace Esforco.Dominio;

public sealed record NomeDePlano(string Plano, string Nome);

public sealed record CadastroDeEscritorio(
    string Id,
    string Nome,
    IReadOnlyList<string> Variacoes,
    IReadOnlyList<NomeDePlano> Planos);

public sealed class NomesDePainel
{
    public static readonly NomesDePainel Vazio = new([]);

    private readonly Dictionary<string, CadastroDeEscritorio> _porId;

    public NomesDePainel(IEnumerable<CadastroDeEscritorio> cadastros)
    {
        ArgumentNullException.ThrowIfNull(cadastros);
        _porId = new Dictionary<string, CadastroDeEscritorio>(StringComparer.Ordinal);
        foreach (var cadastro in cadastros)
        {
            if (cadastro.Id.Length > 0)
                _porId[cadastro.Id] = cadastro;
        }
    }

    public bool Conhece(string id) => id.Length > 0 && _porId.ContainsKey(id);

    public bool Oculto(string? id, string titulo, IEnumerable<OcultacaoDeEscritorio> ocultos)
    {
        ArgumentNullException.ThrowIfNull(ocultos);
        foreach (var item in ocultos)
        {
            if (item.PorId)
            {
                if (!string.IsNullOrEmpty(id) && string.Equals(id, item.Texto.Trim(), StringComparison.Ordinal))
                    return true;
                continue;
            }

            var chave = Normalizar(item.Texto);
            if (chave.Length == 0)
                continue;
            if (Normalizar(titulo) == chave)
                return true;
            if (string.IsNullOrEmpty(id) || !_porId.TryGetValue(id, out var cadastro))
                continue;
            if (cadastro.Nome.Length > 0 && Normalizar(cadastro.Nome) == chave)
                return true;
            foreach (var variacao in cadastro.Variacoes)
            {
                if (Normalizar(variacao) == chave)
                    return true;
            }
        }

        return false;
    }

    public string RotuloDeEscritorio(string id)
    {
        if (id.Length == 0)
            return "Sem escritório";

        if (_porId.TryGetValue(id, out var cadastro) && cadastro.Nome.Length > 0)
            return cadastro.Nome;

        return $"Escritório {id}";
    }

    public string RotuloDePlano(string escritorio, string plano)
    {
        if (plano.Length == 0)
            return "";

        if (_porId.TryGetValue(escritorio, out var cadastro))
        {
            foreach (var item in cadastro.Planos)
            {
                if (item.Plano == plano && item.Nome.Length > 0)
                    return item.Nome;
            }
        }

        return $"Plano {plano}";
    }

    public string? IdDaReuniao(string nomeNaReuniao)
    {
        var chave = Normalizar(nomeNaReuniao);
        if (chave.Length == 0)
            return null;

        string? escolhido = null;
        foreach (var cadastro in _porId.Values)
        {
            if (!Casa(cadastro, chave))
                continue;

            if (escolhido is null || string.CompareOrdinal(cadastro.Id, escolhido) < 0)
                escolhido = cadastro.Id;
        }

        return escolhido;
    }

    public string? IdNoTexto(string? texto)
    {
        var chave = Normalizar(texto);
        if (chave.Length == 0)
            return null;

        string? escolhido = null;
        var tamanho = -1;
        foreach (var cadastro in _porId.Values)
        {
            var trecho = MaiorTrecho(cadastro, chave);
            if (trecho < 0)
                continue;

            if (trecho > tamanho || (trecho == tamanho && (escolhido is null || string.CompareOrdinal(cadastro.Id, escolhido) < 0)))
            {
                tamanho = trecho;
                escolhido = cadastro.Id;
            }
        }

        return escolhido;
    }

    private static int MaiorTrecho(CadastroDeEscritorio cadastro, string chave)
    {
        var melhor = -1;
        Medir(cadastro.Nome);
        foreach (var variacao in cadastro.Variacoes)
            Medir(variacao);
        return melhor;

        void Medir(string bruto)
        {
            var parte = Normalizar(bruto);
            if (parte.Length < 2 || !ContemInteiro(chave, parte))
                return;

            if (parte.Length > melhor)
                melhor = parte.Length;
        }
    }

    private static bool ContemInteiro(string texto, string parte)
    {
        var inicio = 0;
        while (inicio <= texto.Length - parte.Length)
        {
            var indice = texto.IndexOf(parte, inicio, StringComparison.Ordinal);
            if (indice < 0)
                return false;

            var antes = indice == 0 || !char.IsLetterOrDigit(texto[indice - 1]);
            var depois = indice + parte.Length >= texto.Length || !char.IsLetterOrDigit(texto[indice + parte.Length]);
            if (antes && depois)
                return true;

            inicio = indice + 1;
        }

        return false;
    }

    public static string Exibir(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return "";

        var partes = texto.Trim().Split(Separadores, StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', partes);
    }

    private static readonly char[] Separadores = [' ', '\t', '\r', '\n', '\u00A0'];

    public static string Normalizar(string? texto) => Exibir(texto).ToLowerInvariant();

    private static bool Casa(CadastroDeEscritorio cadastro, string chave)
    {
        if (Normalizar(cadastro.Nome) == chave && chave.Length > 0 && cadastro.Nome.Length > 0)
            return true;

        foreach (var variacao in cadastro.Variacoes)
        {
            if (Normalizar(variacao) == chave)
                return true;
        }

        return false;
    }
}

public static class VariacaoDeEscritorio
{
    public static string? Impedir(string? texto, string nomeOficial, IEnumerable<string> existentes)
    {
        var normal = NomesDePainel.Normalizar(texto);
        if (normal.Length == 0)
            return "Escreva a variação.";

        if (NomesDePainel.Normalizar(nomeOficial) == normal && nomeOficial.Trim().Length > 0)
            return "O nome do escritório já cobre essa variação.";

        foreach (var existente in existentes)
        {
            if (NomesDePainel.Normalizar(existente) == normal)
                return "Essa variação já está neste escritório.";
        }

        return null;
    }
}
