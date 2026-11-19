namespace Esforco.Dominio;

public sealed record OcorrenciaDeReuniao(DateTimeOffset Inicio, DateTimeOffset? Fim, TimeSpan Duracao);

public sealed record DiaDeReuniao(string Rotulo, TimeSpan Duracao, IReadOnlyList<OcorrenciaDeReuniao> Ocorrencias);

public sealed record TemaDeReuniao(string Tema, TimeSpan Duracao, IReadOnlyList<DiaDeReuniao> Dias);

public sealed record PlanoConhecido(string Plano, string Nome);

/// <summary>
/// Tempo de vínculo manual em uma categoria. DuracaoNaReuniao é o trecho que a reunião
/// do mesmo escritório já conta: aparece em azul e não entra no total.
/// </summary>
public sealed record OutroTrabalho(string Categoria, TimeSpan Duracao, TimeSpan DuracaoNaReuniao);

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
    TimeSpan DuracaoChatNaReuniao = default,
    TimeSpan DuracaoOutros = default,
    TimeSpan DuracaoOutrosNaReuniao = default,
    IReadOnlyList<OutroTrabalho>? Outros = null)
{
    /// <summary>
    /// O que entra no Total e na Parte: painel, Chatwoot, reuniões e outros trabalhos, sem o azul.
    /// </summary>
    public TimeSpan DuracaoContada => DuracaoPainel + DuracaoChat + DuracaoReunioes + DuracaoOutros;
}

public static class RelatorioDeEscritorios
{
    public static IReadOnlyList<LinhaDeEscritorio> Montar(
        IEnumerable<IntervaloDeEsforco> intervalos,
        DateTimeOffset agora,
        IEnumerable<CadastroDeEscritorio>? cadastros = null,
        IEnumerable<IntervaloDeEsforco>? historico = null,
        ContextoDeVinculos? vinculos = null)
    {
        ArgumentNullException.ThrowIfNull(intervalos);
        var gravados = intervalos.ToList();
        var todos = historico?.ToList() ?? gravados;
        var lista = cadastros?.ToList() ?? [];
        var nomes = new NomesDePainel(lista);
        var porId = lista.Where(item => item.Id.Length > 0).ToDictionary(item => item.Id, StringComparer.Ordinal);

        var trechos = AplicadorDeVinculos.Aplicar(gravados, vinculos, nomes, agora, todos);
        var periodo = trechos.Where(trecho => trecho.Vinculo is null).Select(trecho => trecho.Intervalo).ToList();
        var vinculados = trechos.Where(trecho => trecho.Vinculo is not null).ToList();

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

        foreach (var trecho in vinculados)
            ids.Add(trecho.Vinculo!.Escritorio);

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

            IReadOnlyList<IntervaloDeEsforco> reunioesDoEscritorio = grupo is null
                ? []
                : grupo.Select(item => item.Intervalo).ToList();

            var chats = periodo.Where(intervalo => EhChat(intervalo, id)).ToList();
            if (chats.Count > 0)
            {
                var (chatContado, chatNaReuniao) = PainelDuranteReuniao.Separar(chats, reunioesDoEscritorio, agora);
                linhas[i] = linhas[i] with
                {
                    DuracaoChat = chatContado,
                    DuracaoChatNaReuniao = chatNaReuniao,
                };
            }

            var doEscritorio = vinculados.Where(trecho => trecho.Vinculo!.Escritorio == id).ToList();
            var outros = OutrosDe(
                doEscritorio,
                reunioesDoEscritorio,
                doEscritorio.Any(trecho => trecho.Vinculo!.Manual)
                    ? periodo.Where(intervalo => intervalo.Escritorio.Trim() == id && (intervalo.EhPainelSplitC || EhChat(intervalo, id))).ToList()
                    : [],
                agora);
            if (outros.Count > 0)
            {
                linhas[i] = linhas[i] with
                {
                    DuracaoOutros = Somar(outros.Select(item => item.Duracao)),
                    DuracaoOutrosNaReuniao = Somar(outros.Select(item => item.DuracaoNaReuniao)),
                    Outros = outros,
                };
            }
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
            .OrderByDescending(linha => linha.DuracaoContada > TimeSpan.Zero)
            .ThenByDescending(linha => linha.DuracaoContada)
            .ThenBy(linha => linha.Rotulo, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// O trabalho vinculado só deixa de contar durante a reunião do mesmo escritório. O registro manual
    /// completa o que o Esforço não viu: deixa de contar no horário que qualquer outro registro do mesmo
    /// escritório já conta, seja reunião, painel, Chatwoot, trabalho vinculado ou outro registro manual.
    /// </summary>
    private static List<OutroTrabalho> OutrosDe(
        IReadOnlyList<TrechoDeEsforco> vinculados,
        IReadOnlyList<IntervaloDeEsforco> reunioes,
        IReadOnlyList<IntervaloDeEsforco> painelEChat,
        DateTimeOffset agora)
    {
        var partes = new List<(string Categoria, TimeSpan Contado, TimeSpan NaReuniao)>();
        var automaticos = new List<IntervaloDeEsforco>(reunioes);
        automaticos.AddRange(painelEChat);
        foreach (var trecho in vinculados)
        {
            if (trecho.Vinculo!.Manual)
                continue;

            var (contado, naReuniao) = PainelDuranteReuniao.Separar([trecho.Intervalo], reunioes, agora);
            partes.Add((trecho.Vinculo.Categoria, contado, naReuniao));
            automaticos.Add(trecho.Intervalo);
        }

        var manuais = vinculados.Where(trecho => trecho.Vinculo!.Manual).ToList();
        if (manuais.Count > 0)
        {
            var ordenados = PainelDuranteReuniao.EmOrdem(manuais.Select(trecho => trecho.Intervalo)).ToList();
            var separados = PainelDuranteReuniao.SepararManuais(ordenados, automaticos, agora);
            for (var i = 0; i < ordenados.Count; i++)
                partes.Add((ordenados[i].Manual?.Categoria ?? "", separados[i].Contado, separados[i].JaContado));
        }

        return partes
            .GroupBy(parte => parte.Categoria, StringComparer.OrdinalIgnoreCase)
            .Select(grupo => new OutroTrabalho(
                grupo.Key.Length == 0 ? CadastroDeCategorias.SemCategoria : grupo.First().Categoria,
                Somar(grupo.Select(parte => parte.Contado)),
                Somar(grupo.Select(parte => parte.NaReuniao))))
            .OrderByDescending(item => item.Duracao + item.DuracaoNaReuniao)
            .ThenBy(item => item.Categoria, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static TimeSpan Somar(IEnumerable<TimeSpan> duracoes)
    {
        var total = TimeSpan.Zero;
        foreach (var duracao in duracoes)
            total += duracao;
        return total;
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
