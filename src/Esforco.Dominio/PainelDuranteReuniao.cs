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

    public static void Marcar(IEnumerable<IntervaloDeEsforco> intervalos, NomesDePainel nomes, DateTimeOffset agora)
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
    }

    private static DateTimeOffset FimDe(IntervaloDeEsforco intervalo, DateTimeOffset agora)
    {
        var fim = intervalo.Fim ?? agora;
        return fim < intervalo.Inicio ? intervalo.Inicio : fim;
    }

    private static List<(DateTimeOffset Inicio, DateTimeOffset Fim)> Faixas(
        IEnumerable<IntervaloDeEsforco> reunioes,
        DateTimeOffset agora)
    {
        var ordenadas = reunioes
            .Select(item => (item.Inicio, Fim: FimDe(item, agora)))
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
