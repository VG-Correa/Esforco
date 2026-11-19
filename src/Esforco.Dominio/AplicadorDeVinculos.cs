namespace Esforco.Dominio;

public sealed record ContextoDeVinculos(
    IReadOnlyList<VinculoDeEscritorio> Vinculos,
    IReadOnlyList<Categoria> Categorias,
    IReadOnlyDictionary<long, string> Enderecos)
{
    public static ContextoDeVinculos Vazio { get; } = new([], [], new Dictionary<long, string>());
}

/// <summary>
/// Manual é verdadeiro quando o escritório vem de um registro manual, e não de um vínculo: aí VinculoId é zero.
/// </summary>
public sealed record VinculoAplicado(long VinculoId, string Escritorio, long? CategoriaId, string Categoria, bool Manual = false);

/// <summary>
/// Um pedaço de intervalo. Origem é o intervalo gravado; Intervalo é o pedaço, igual à origem
/// quando não houve corte. Vinculo fica vazio onde nenhum vínculo manual vale.
/// </summary>
public sealed record TrechoDeEsforco(IntervaloDeEsforco Origem, IntervaloDeEsforco Intervalo, VinculoAplicado? Vinculo);

/// <summary>
/// Aplica os vínculos manuais aos intervalos, em memória. O banco não é regravado.
/// </summary>
public static class AplicadorDeVinculos
{
    public static IReadOnlyList<TrechoDeEsforco> Aplicar(
        IEnumerable<IntervaloDeEsforco> intervalos,
        ContextoDeVinculos? contexto,
        NomesDePainel nomes,
        DateTimeOffset agora,
        IEnumerable<IntervaloDeEsforco>? historico = null)
    {
        ArgumentNullException.ThrowIfNull(intervalos);
        ArgumentNullException.ThrowIfNull(nomes);
        var lista = intervalos.ToList();
        var trechos = new List<TrechoDeEsforco>(lista.Count);
        if (contexto is null || contexto.Vinculos.Count == 0)
        {
            foreach (var intervalo in lista)
                trechos.Add(new TrechoDeEsforco(intervalo, intervalo, DoRegistroManual(intervalo)));
            return trechos;
        }

        var todos = historico?.ToList() ?? lista;
        var categorias = contexto.Categorias.ToDictionary(item => item.Id, item => item.Nome);
        var validos = new List<Candidato>();
        foreach (var vinculo in contexto.Vinculos)
        {
            var janela = ValidadeDeVinculo.Calcular(vinculo, todos, contexto.Enderecos, agora);
            if (janela.Vazia)
                continue;

            var categoria = vinculo.CategoriaId is long id && categorias.TryGetValue(id, out var nome) ? nome : "";
            validos.Add(new Candidato(
                vinculo,
                janela,
                new VinculoAplicado(vinculo.Id, vinculo.Escritorio, vinculo.CategoriaId, categoria)));
        }

        foreach (var intervalo in lista)
        {
            if (DoRegistroManual(intervalo) is VinculoAplicado manual)
                trechos.Add(new TrechoDeEsforco(intervalo, intervalo, manual));
            else
                Repartir(intervalo, validos, contexto.Enderecos, nomes, agora, trechos);
        }

        return trechos;
    }

    /// <summary>
    /// O registro manual já diz o escritório e a categoria: nenhum vínculo é procurado para ele.
    /// </summary>
    private static VinculoAplicado? DoRegistroManual(IntervaloDeEsforco intervalo) =>
        intervalo.Manual is MarcaDeRegistroManual marca
            ? new VinculoAplicado(0, intervalo.Escritorio, marca.CategoriaId, marca.Categoria, true)
            : null;

    private static void Repartir(
        IntervaloDeEsforco intervalo,
        List<Candidato> validos,
        IReadOnlyDictionary<long, string> enderecos,
        NomesDePainel nomes,
        DateTimeOffset agora,
        List<TrechoDeEsforco> trechos)
    {
        var inicio = intervalo.Inicio;
        var fim = intervalo.Fim ?? agora;
        if (fim < inicio)
            fim = inicio;

        List<Candidato>? candidatos = null;
        var endereco = ValidadeDeVinculo.Endereco(intervalo, enderecos);
        foreach (var candidato in validos)
        {
            if (candidato.Janela.Inicio > fim || (candidato.Janela.Fim is DateTimeOffset limite && limite < inicio))
                continue;
            if (!candidato.Vinculo.Casa(intervalo, endereco))
                continue;

            candidatos ??= [];
            candidatos.Add(candidato);
        }

        if (candidatos is null || ResolvedorDeEscritorio.Descoberto(intervalo, nomes))
        {
            trechos.Add(new TrechoDeEsforco(intervalo, intervalo, null));
            return;
        }

        if (fim == inicio)
        {
            var noInstante = Vencedor(candidatos.Where(item => item.Janela.Vale(inicio)));
            trechos.Add(new TrechoDeEsforco(intervalo, intervalo, noInstante?.Aplicado));
            return;
        }

        var cortes = new SortedSet<DateTimeOffset> { inicio, fim };
        foreach (var candidato in candidatos)
        {
            if (candidato.Janela.Inicio > inicio && candidato.Janela.Inicio < fim)
                cortes.Add(candidato.Janela.Inicio);
            if (candidato.Janela.Fim is DateTimeOffset limite && limite > inicio && limite < fim)
                cortes.Add(limite);
        }

        var pontos = cortes.ToList();
        var pedacos = new List<(DateTimeOffset De, DateTimeOffset Ate, Candidato? Dono)>();
        for (var i = 0; i < pontos.Count - 1; i++)
        {
            var de = pontos[i];
            var ate = pontos[i + 1];
            var dono = Vencedor(candidatos.Where(item => item.Janela.Cobre(de, ate)));
            if (pedacos.Count > 0 && ReferenceEquals(pedacos[^1].Dono, dono))
                pedacos[^1] = (pedacos[^1].De, ate, dono);
            else
                pedacos.Add((de, ate, dono));
        }

        if (pedacos.Count == 1)
        {
            trechos.Add(new TrechoDeEsforco(intervalo, intervalo, pedacos[0].Dono?.Aplicado));
            return;
        }

        for (var i = 0; i < pedacos.Count; i++)
        {
            var (de, ate, dono) = pedacos[i];
            var aberto = i == pedacos.Count - 1 && intervalo.Fim is null;
            trechos.Add(new TrechoDeEsforco(intervalo, intervalo.Recortar(de, aberto ? null : ate), dono?.Aplicado));
        }
    }

    private static Candidato? Vencedor(IEnumerable<Candidato> candidatos)
    {
        var lista = candidatos.ToList();
        if (lista.Count == 0)
            return null;

        var escolhido = ResolvedorDeEscritorio.Escolher(lista.Select(item => item.Vinculo));
        return lista.First(item => ReferenceEquals(item.Vinculo, escolhido));
    }

    private sealed record Candidato(VinculoDeEscritorio Vinculo, JanelaDeValidade Janela, VinculoAplicado Aplicado);
}
