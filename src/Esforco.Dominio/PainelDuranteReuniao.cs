namespace Esforco.Dominio;

public static class PainelDuranteReuniao
{
    public static (TimeSpan Contado, TimeSpan NaReuniao) Separar(
        IEnumerable<IntervaloDeEsforco> paineis,
        IEnumerable<IntervaloDeEsforco> reunioes,
        DateTimeOffset agora)
    {
        ArgumentNullException.ThrowIfNull(paineis);
        ArgumentNullException.ThrowIfNull(reunioes);
        var faixas = Faixas(reunioes, agora);
        var contado = TimeSpan.Zero;
        var naReuniao = TimeSpan.Zero;
        foreach (var painel in paineis)
        {
            var duracao = painel.DuracaoAte(agora);
            var cruza = Cruzamento(painel.Inicio, FimDe(painel, agora), faixas);
            if (cruza > duracao)
                cruza = duracao;

            naReuniao += cruza;
            contado += duracao - cruza;
        }

        return (contado, naReuniao);
    }

    /// <summary>
    /// Marca em azul o painel e o Chatwoot usados durante a reunião do mesmo escritório.
    /// Com os trechos, o trabalho vinculado à mão entra na mesma regra.
    /// </summary>
    public static void Marcar(
        IEnumerable<IntervaloDeEsforco> intervalos,
        NomesDePainel nomes,
        DateTimeOffset agora,
        IEnumerable<TrechoDeEsforco>? trechos = null)
    {
        ArgumentNullException.ThrowIfNull(intervalos);
        ArgumentNullException.ThrowIfNull(nomes);
        var lista = intervalos.ToList();
        var porId = new Dictionary<string, List<IntervaloDeEsforco>>(StringComparer.Ordinal);
        foreach (var intervalo in lista)
        {
            if (intervalo.EhPainelSplitC || intervalo.EhChatwoot)
                intervalo.MarcarDuranteReuniao(false);

            var lida = InterpretadorDeReuniao.Ler(intervalo.Titulo);
            if (lida is null)
                continue;

            var id = nomes.IdDaReuniao(lida.Nome);
            if (id is null)
                continue;

            if (!porId.TryGetValue(id, out var grupo))
            {
                grupo = [];
                porId[id] = grupo;
            }

            grupo.Add(intervalo);
        }

        foreach (var intervalo in lista)
        {
            if (!intervalo.EhPainelSplitC && !intervalo.EhChatwoot)
                continue;

            var id = intervalo.Escritorio.Trim();
            if (id.Length == 0 || !porId.TryGetValue(id, out var reunioes))
                continue;

            if (Cruzamento(intervalo.Inicio, FimDe(intervalo, agora), Faixas(reunioes, agora)) > TimeSpan.Zero)
                intervalo.MarcarDuranteReuniao(true);
        }

        if (trechos is null)
            return;

        var pedacos = trechos.ToList();
        foreach (var trecho in pedacos)
        {
            trecho.Origem.MarcarVinculo(null);
            if (!trecho.Origem.EhPainelSplitC && !trecho.Origem.EhChatwoot)
                trecho.Origem.MarcarDuranteReuniao(false);
        }

        foreach (var trecho in pedacos)
        {
            if (trecho.Vinculo is not VinculoAplicado vinculo)
                continue;

            trecho.Origem.MarcarVinculo(nomes.RotuloDeEscritorio(vinculo.Escritorio));
            if (!porId.TryGetValue(vinculo.Escritorio, out var reunioes))
                continue;

            var pedaco = trecho.Intervalo;
            if (Cruzamento(pedaco.Inicio, FimDe(pedaco, agora), Faixas(reunioes, agora)) > TimeSpan.Zero)
                trecho.Origem.MarcarDuranteReuniao(true);
        }

        MarcarManuais(pedacos, porId, agora);
    }

    /// <summary>
    /// Para cada registro manual de um escritório, na ordem de <see cref="EmOrdem"/>, quanto conta e quanto
    /// outro registro do mesmo escritório já conta no mesmo horário: os automáticos e os manuais anteriores.
    /// </summary>
    public static IReadOnlyList<(TimeSpan Contado, TimeSpan JaContado)> SepararManuais(
        IEnumerable<IntervaloDeEsforco> manuais,
        IEnumerable<IntervaloDeEsforco> automaticos,
        DateTimeOffset agora)
    {
        ArgumentNullException.ThrowIfNull(manuais);
        ArgumentNullException.ThrowIfNull(automaticos);
        var faixas = Faixas(automaticos, agora);
        var resultado = new List<(TimeSpan Contado, TimeSpan JaContado)>();
        foreach (var manual in manuais)
        {
            var fim = FimDe(manual, agora);
            var duracao = manual.DuracaoAte(agora);
            var cruza = Cruzamento(manual.Inicio, fim, faixas);
            if (cruza > duracao)
                cruza = duracao;

            resultado.Add((duracao - cruza, cruza));
            if (fim > manual.Inicio)
                faixas = Unir(faixas.Append((manual.Inicio, fim)));
        }

        return resultado;
    }

    /// <summary>
    /// Entre dois registros manuais do mesmo escritório no mesmo horário, conta o que começa antes
    /// e, no empate, o que foi criado antes.
    /// </summary>
    public static IEnumerable<IntervaloDeEsforco> EmOrdem(IEnumerable<IntervaloDeEsforco> manuais) =>
        manuais.OrderBy(item => item.Inicio).ThenBy(item => item.Manual?.Id ?? 0);

    private static void MarcarManuais(
        List<TrechoDeEsforco> pedacos,
        Dictionary<string, List<IntervaloDeEsforco>> reunioes,
        DateTimeOffset agora)
    {
        var manuais = pedacos.Where(trecho => trecho.Vinculo is { Manual: true }).ToList();
        if (manuais.Count == 0)
            return;

        var automaticos = new Dictionary<string, List<IntervaloDeEsforco>>(StringComparer.Ordinal);
        foreach (var (id, lista) in reunioes)
            automaticos[id] = [.. lista];

        foreach (var trecho in pedacos)
        {
            string id;
            if (trecho.Vinculo is VinculoAplicado vinculo)
            {
                if (vinculo.Manual)
                    continue;
                id = vinculo.Escritorio;
            }
            else if (trecho.Intervalo.EhPainelSplitC || trecho.Intervalo.EhChatwoot)
            {
                id = trecho.Intervalo.Escritorio.Trim();
            }
            else
            {
                continue;
            }

            if (id.Length == 0)
                continue;

            if (!automaticos.TryGetValue(id, out var grupo))
            {
                grupo = [];
                automaticos[id] = grupo;
            }

            grupo.Add(trecho.Intervalo);
        }

        foreach (var grupo in manuais.GroupBy(trecho => trecho.Vinculo!.Escritorio, StringComparer.Ordinal))
        {
            var ordenados = EmOrdem(grupo.Select(trecho => trecho.Origem)).ToList();
            var separados = SepararManuais(
                ordenados,
                automaticos.TryGetValue(grupo.Key, out var outros) ? outros : [],
                agora);
            for (var i = 0; i < ordenados.Count; i++)
            {
                if (separados[i].JaContado > TimeSpan.Zero)
                    ordenados[i].MarcarDuranteReuniao(true);
            }
        }
    }

    private static DateTimeOffset FimDe(IntervaloDeEsforco intervalo, DateTimeOffset agora)
    {
        var fim = intervalo.Fim ?? agora;
        return fim < intervalo.Inicio ? intervalo.Inicio : fim;
    }

    private static List<(DateTimeOffset Inicio, DateTimeOffset Fim)> Faixas(
        IEnumerable<IntervaloDeEsforco> reunioes,
        DateTimeOffset agora) =>
        Unir(reunioes.Select(item => (item.Inicio, Fim: FimDe(item, agora))));

    private static List<(DateTimeOffset Inicio, DateTimeOffset Fim)> Unir(
        IEnumerable<(DateTimeOffset Inicio, DateTimeOffset Fim)> itens)
    {
        var ordenadas = itens
            .Where(item => item.Fim > item.Inicio)
            .OrderBy(item => item.Inicio)
            .ThenBy(item => item.Fim)
            .ToList();
        var faixas = new List<(DateTimeOffset Inicio, DateTimeOffset Fim)>();
        foreach (var item in ordenadas)
        {
            if (faixas.Count == 0 || item.Inicio > faixas[^1].Fim)
            {
                faixas.Add(item);
                continue;
            }

            if (item.Fim > faixas[^1].Fim)
                faixas[^1] = (faixas[^1].Inicio, item.Fim);
        }

        return faixas;
    }

    private static TimeSpan Cruzamento(
        DateTimeOffset inicio,
        DateTimeOffset fim,
        List<(DateTimeOffset Inicio, DateTimeOffset Fim)> faixas)
    {
        var total = TimeSpan.Zero;
        foreach (var faixa in faixas)
        {
            var de = inicio > faixa.Inicio ? inicio : faixa.Inicio;
            var ate = fim < faixa.Fim ? fim : faixa.Fim;
            if (ate > de)
                total += ate - de;
        }

        return total;
    }
}
