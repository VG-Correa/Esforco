namespace Esforco.Dominio;

public sealed record OcultacaoDeEscritorio(bool PorId, string Texto);

public static class ListaNegraDeEscritorios
{
    public static IReadOnlyList<LinhaDeEscritorio> Visiveis(
        IEnumerable<LinhaDeEscritorio> linhas,
        IEnumerable<OcultacaoDeEscritorio> ocultacoes)
    {
        ArgumentNullException.ThrowIfNull(linhas);
        ArgumentNullException.ThrowIfNull(ocultacoes);
        var lista = ocultacoes.ToList();
        return linhas.Where(linha => !Some(linha, lista)).ToList();
    }

    public static string? Impedir(bool porId, string? texto, IEnumerable<OcultacaoDeEscritorio> existentes)
    {
        ArgumentNullException.ThrowIfNull(existentes);
        var limpo = porId ? (texto ?? "").Trim() : NomesDePainel.Exibir(texto);
        if (limpo.Length == 0)
            return porId ? "Informe o id." : "Informe o nome.";

        foreach (var item in existentes)
        {
            if (item.PorId != porId)
                continue;

            var igual = porId
                ? string.Equals(item.Texto.Trim(), limpo, StringComparison.Ordinal)
                : NomesDePainel.Normalizar(item.Texto) == NomesDePainel.Normalizar(limpo);
            if (igual)
                return "Esse escritório já está oculto.";
        }

        return null;
    }

    private static bool Some(LinhaDeEscritorio linha, IReadOnlyList<OcultacaoDeEscritorio> ocultacoes)
    {
        foreach (var item in ocultacoes)
        {
            if (item.PorId)
            {
                if (linha.Id.Length > 0 && string.Equals(linha.Id, item.Texto.Trim(), StringComparison.Ordinal))
                    return true;
                continue;
            }

            var chave = NomesDePainel.Normalizar(item.Texto);
            if (chave.Length == 0)
                continue;

            if (NomesDePainel.Normalizar(linha.Nome) == chave && linha.Nome.Length > 0)
                return true;
            if (NomesDePainel.Normalizar(linha.Rotulo) == chave)
                return true;
            foreach (var variacao in linha.Variacoes)
            {
                if (NomesDePainel.Normalizar(variacao) == chave)
                    return true;
            }
        }

        return false;
    }
}
