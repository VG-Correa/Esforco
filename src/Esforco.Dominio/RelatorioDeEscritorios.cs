namespace Esforco.Dominio;

public sealed record OcorrenciaDeReuniao(DateTimeOffset Inicio, DateTimeOffset? Fim, TimeSpan Duracao);

public sealed record DiaDeReuniao(string Rotulo, TimeSpan Duracao, IReadOnlyList<OcorrenciaDeReuniao> Ocorrencias);

public sealed record TemaDeReuniao(string Tema, TimeSpan Duracao, IReadOnlyList<DiaDeReuniao> Dias);

public sealed record PlanoConhecido(string Plano, string Nome);

public sealed record LinhaDeEscritorio(
    string Chave,
    string Id,
    string Rotulo,
    string Nome,
    IReadOnlyList<string> Variacoes,
    TimeSpan DuracaoPainel,
    TimeSpan DuracaoPainelNaReuniao,
    int IntervalosPainel,
    TimeSpan DuracaoReunioes,
    IReadOnlyList<TemaDeReuniao> Temas,
    IReadOnlyList<PlanoConhecido> Planos,
    bool Editavel,
    TimeSpan DuracaoChat = default,
    TimeSpan DuracaoChatNaReuniao = default);

public static class RelatorioDeEscritorios
{
    public static IReadOnlyList<LinhaDeEscritorio> Montar(
        IEnumerable<IntervaloDeEsforco> intervalos,
        DateTimeOffset agora,
        IEnumerable<CadastroDeEscritorio>? cadastros = null,
        IEnumerable<IntervaloDeEsforco>? historico = null)
    {
        ArgumentNullException.ThrowIfNull(intervalos);
        var periodo = intervalos.ToList();
        var todos = historico?.ToList() ?? periodo;
        var lista = cadastros?.ToList() ?? [];
        var nomes = new NomesDePainel(lista);
        var porId = lista.Where(item => item.Id.Length > 0).ToDictionary(item => item.Id, StringComparer.Ordinal);

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var cadastro in lista)
        {
            if (cadastro.Id.Length > 0)
                ids.Add(cadastro.Id);
        }

        foreach (var intervalo in todos)
        {
            if (intervalo.EhPainelSplitC && intervalo.Escritorio.Trim().Length > 0)
                ids.Add(intervalo.Escritorio.Trim());
        }

        var linhas = ids
            .Select(id => LinhaDoPainel(id, porId, periodo, todos, nomes, agora))
            .ToList();

        var reunioes = periodo
            .Where(intervalo => !intervalo.EhPainelSplitC)
            .Select(intervalo => (intervalo, lida: InterpretadorDeReuniao.Ler(intervalo.Titulo)))
            .Where(item => item.lida is not null)
            .ToList();

        var porEscritorio = new Dictionary<string, List<(IntervaloDeEsforco Intervalo, ReuniaoLida Lida)>>(StringComparer.Ordinal);
        var semDono = new Dictionary<string, List<(IntervaloDeEsforco Intervalo, ReuniaoLida Lida)>>(StringComparer.Ordinal);

        foreach (var item in reunioes)
        {
            var id = nomes.IdDaReuniao(item.lida!.Nome);
            if (id is null)
            {
                var chave = NomesDePainel.Normalizar(item.lida.Nome);
                if (!semDono.TryGetValue(chave, out var grupo))
                {
                    grupo = [];
                    semDono[chave] = grupo;
                }

                grupo.Add((item.intervalo, item.lida));
                continue;
            }

            if (!porEscritorio.TryGetValue(id, out var doEscritorio))
            {
                doEscritorio = [];
                porEscritorio[id] = doEscritorio;
            }

            doEscritorio.Add((item.intervalo, item.lida));
        }

        for (var i = 0; i < linhas.Count; i++)
        {
            var id = linhas[i].Id;
            porEscritorio.TryGetValue(id, out var grupo);
            if (grupo is not null)
            {
                var paineis = periodo.Where(intervalo => intervalo.EhPainelSplitC && intervalo.Escritorio.Trim() == id);
                var (contado, naReuniao) = PainelDuranteReuniao.Separar(paineis, grupo.Select(item => item.Intervalo), agora);
                linhas[i] = ComReunioes(linhas[i], TemasDe(grupo, agora)) with
                {
                    DuracaoPainel = contado,
                    DuracaoPainelNaReuniao = naReuniao,
                };
            }

            var chats = periodo.Where(intervalo => EhChat(intervalo, id)).ToList();
            if (chats.Count == 0)
                continue;

            IReadOnlyList<IntervaloDeEsforco> reunioesDoEscritorio = grupo is null
                ? []
                : grupo.Select(item => item.Intervalo).ToList();
            var (chatContado, chatNaReuniao) = PainelDuranteReuniao.Separar(chats, reunioesDoEscritorio, agora);
            linhas[i] = linhas[i] with
            {
                DuracaoChat = chatContado,
                DuracaoChatNaReuniao = chatNaReuniao,
            };
        }

        foreach (var grupo in semDono.Values)
        {
            var temas = TemasDe(grupo, agora);
            var rotulo = grupo[0].Lida.Nome;
            linhas.Add(new LinhaDeEscritorio(
                "reuniao:" + NomesDePainel.Normalizar(rotulo),
                "",
                rotulo,
                "",
                [],
                TimeSpan.Zero,
                TimeSpan.Zero,
                0,
                Somar(temas),
                temas,
                [],
                false));
        }

        return linhas
            .OrderByDescending(linha => linha.DuracaoPainel + linha.DuracaoReunioes + linha.DuracaoChat > TimeSpan.Zero)
            .ThenByDescending(linha => linha.DuracaoPainel + linha.DuracaoReunioes + linha.DuracaoChat)
            .ThenBy(linha => linha.Rotulo, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static LinhaDeEscritorio LinhaDoPainel(
        string id,
        IReadOnlyDictionary<string, CadastroDeEscritorio> porId,
        IReadOnlyList<IntervaloDeEsforco> periodo,
        IReadOnlyList<IntervaloDeEsforco> historico,
        NomesDePainel nomes,
        DateTimeOffset agora)
    {
        porId.TryGetValue(id, out var cadastro);
        var doPeriodo = periodo
            .Where(intervalo => intervalo.EhPainelSplitC && intervalo.Escritorio.Trim() == id)
            .ToList();
        var planos = historico
            .Where(intervalo => intervalo.EhPainelSplitC && intervalo.Escritorio.Trim() == id && intervalo.Plano.Trim().Length > 0)
            .Select(intervalo => intervalo.Plano.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(plano => plano, StringComparer.Ordinal)
            .Select(plano => new PlanoConhecido(plano, NomeDoPlano(cadastro, plano)))
            .ToList();

        return new LinhaDeEscritorio(
            "id:" + id,
            id,
            nomes.RotuloDeEscritorio(id),
            cadastro?.Nome ?? "",
            (cadastro?.Variacoes ?? []).OrderBy(item => item, StringComparer.OrdinalIgnoreCase).ToList(),
            Somar(doPeriodo, agora),
            TimeSpan.Zero,
            doPeriodo.Count,
            TimeSpan.Zero,
            [],
            planos,
            true);
    }

    private static bool EhChat(IntervaloDeEsforco intervalo, string id) =>
        intervalo.Escritorio.Trim() == id
        && !intervalo.EhPainelSplitC
        && (string.Equals(intervalo.Programa, InterpretadorDeChatwoot.NomeDoPrograma, StringComparison.Ordinal)
            || InterpretadorDeChatwoot.EhAba(intervalo.Titulo, null));

    private static string NomeDoPlano(CadastroDeEscritorio? cadastro, string plano)
    {
        if (cadastro is null)
            return "";

        foreach (var item in cadastro.Planos)
        {
            if (item.Plano == plano)
                return item.Nome;
        }

        return "";
    }

    private static LinhaDeEscritorio ComReunioes(LinhaDeEscritorio linha, IReadOnlyList<TemaDeReuniao> temas) =>
        linha with { Temas = temas, DuracaoReunioes = Somar(temas) };

    private static List<TemaDeReuniao> TemasDe(
        IEnumerable<(IntervaloDeEsforco Intervalo, ReuniaoLida Lida)> itens,
        DateTimeOffset agora)
    {
        return itens
            .GroupBy(item => NomesDePainel.Normalizar(item.Lida.Tema))
            .Select(grupo =>
            {
                var ocorrencias = Juntar(grupo.Select(item => item.Intervalo).OrderBy(item => item.Inicio).ThenBy(item => item.Id), agora);
                var dias = ocorrencias
                    .GroupBy(ocorrencia => ocorrencia.Inicio.ToLocalTime().Date)
                    .OrderByDescending(dia => dia.Key)
                    .Select(dia => new DiaDeReuniao(
                        RotuloDeDia.De(dia.First().Inicio, agora),
                        Somar(dia),
                        dia.OrderByDescending(ocorrencia => ocorrencia.Inicio).ToList()))
                    .ToList();
                return new TemaDeReuniao(grupo.First().Lida.Tema, Somar(dias), dias);
            })
            .OrderByDescending(tema => tema.Duracao)
            .ThenBy(tema => tema.Tema, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<OcorrenciaDeReuniao> Juntar(IEnumerable<IntervaloDeEsforco> ordenados, DateTimeOffset agora)
    {
        var lista = new List<(DateTimeOffset Inicio, DateTimeOffset? Fim, TimeSpan Duracao)>();
        foreach (var intervalo in ordenados)
        {
            if (lista.Count == 0)
            {
                lista.Add((intervalo.Inicio, intervalo.Fim, intervalo.DuracaoAte(agora)));
                continue;
            }

            var ultimo = lista[^1];
            var fimEfetivo = ultimo.Fim ?? agora;
            if (intervalo.Retomado || intervalo.Inicio > fimEfetivo + ContinuidadeDeReuniao.Folga)
            {
                lista.Add((intervalo.Inicio, intervalo.Fim, intervalo.DuracaoAte(agora)));
                continue;
            }

            DateTimeOffset? fim = ultimo.Fim is null || intervalo.Fim is null
                ? null
                : intervalo.Fim.Value > ultimo.Fim.Value ? intervalo.Fim : ultimo.Fim;
            var duracao = fim is null ? agora - ultimo.Inicio : fim.Value - ultimo.Inicio;
            if (duracao < TimeSpan.Zero)
                duracao = TimeSpan.Zero;
            lista[^1] = (ultimo.Inicio, fim, duracao);
        }

        return lista
            .Select(item => new OcorrenciaDeReuniao(item.Inicio, item.Fim, item.Duracao))
            .ToList();
    }

    private static TimeSpan Somar(IEnumerable<TemaDeReuniao> temas)
    {
        var total = TimeSpan.Zero;
        foreach (var tema in temas)
            total += tema.Duracao;
        return total;
    }

    private static TimeSpan Somar(IEnumerable<DiaDeReuniao> dias)
    {
        var total = TimeSpan.Zero;
        foreach (var dia in dias)
            total += dia.Duracao;
        return total;
    }

    private static TimeSpan Somar(IEnumerable<OcorrenciaDeReuniao> ocorrencias)
    {
        var total = TimeSpan.Zero;
        foreach (var ocorrencia in ocorrencias)
            total += ocorrencia.Duracao;
        return total;
    }

    private static TimeSpan Somar(IEnumerable<IntervaloDeEsforco> intervalos, DateTimeOffset agora)
    {
        var total = TimeSpan.Zero;
        foreach (var intervalo in intervalos)
            total += intervalo.DuracaoAte(agora);
        return total;
    }

}
