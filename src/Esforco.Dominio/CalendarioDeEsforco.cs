namespace Esforco.Dominio;

public enum ModoDoCalendario
{
    Escritorios,
    Programas,
    Telas,
}

public sealed record BlocoDeCalendario(
    int InicioMinuto,
    int FimMinuto,
    int InicioSegundo,
    int FimSegundo,
    int Trilha,
    string Titulo,
    string Subtitulo,
    string Tipo,
    string Chave,
    string Dica,
    AlvoDeVinculo? Alvo = null);

public sealed record LinhaDoDia(string Titulo, string Duracao, string Chave, int Minutos);

public sealed record DiaDeCalendario(
    DateTime Dia,
    string Rotulo,
    int Numero,
    bool NoPeriodo,
    bool EhHoje,
    string Duracao,
    IReadOnlyList<BlocoDeCalendario> Blocos,
    IReadOnlyList<LinhaDoDia> Linhas);

public sealed record QuadroDeCalendario(
    bool EmGrade,
    bool Vertical,
    int HoraInicial,
    int HoraFinal,
    IReadOnlyList<string> Cabecalhos,
    IReadOnlyList<DiaDeCalendario> Dias);

public static class CalendarioDeEsforco
{
    private const int FolgaMinutos = 5;
    public static QuadroDeCalendario Montar(
        IEnumerable<IntervaloDeEsforco> intervalos,
        DateTime inicioPeriodo,
        DateTime fimPeriodo,
        DateTimeOffset agora,
        ModoDoCalendario modo,
        NomesDePainel nomes,
        bool emGrade,
        bool deDomingoASabado,
        IEnumerable<OcultacaoDeEscritorio>? ocultos = null,
        ContextoDeVinculos? vinculos = null)
    {
        ArgumentNullException.ThrowIfNull(intervalos);
        ArgumentNullException.ThrowIfNull(nomes);
        var listaOcultos = ocultos?.ToList() ?? [];
        var enderecos = vinculos?.Enderecos;

        var inicio = inicioPeriodo.Date;
        var fim = fimPeriodo.Date;
        if (fim < inicio)
            (inicio, fim) = (fim, inicio);

        var grade = emGrade || (fim - inicio).Days >= 7;
        var fatias = new List<Fatia>();
        foreach (var trecho in AplicadorDeVinculos.Aplicar(intervalos, vinculos, nomes, agora))
        {
            if (!TentarDescrever(trecho, modo, nomes, listaOcultos, enderecos, out var descricao))
                continue;

            foreach (var fatia in Fatiar(trecho.Intervalo, inicio, fim, agora, descricao))
                fatias.Add(fatia);
        }

        var porDia = new Dictionary<DateTime, List<Fatia>>();
        foreach (var fatia in fatias)
        {
            if (!porDia.TryGetValue(fatia.Dia, out var grupo))
            {
                grupo = [];
                porDia[fatia.Dia] = grupo;
            }

            grupo.Add(fatia);
        }

        var datas = grade ? EnumerarGrade(inicio, fim, deDomingoASabado) : Enumerar(inicio, fim);
        var hoje = agora.ToLocalTime().Date;
        var dias = new List<DiaDeCalendario>(datas.Count);
        foreach (var dia in datas)
        {
            porDia.TryGetValue(dia, out var doDia);
            dias.Add(MontarDia(dia, inicio, fim, hoje, agora, doDia ?? []));
        }

        var (horaInicial, horaFinal) = Janela(dias);
        return new QuadroDeCalendario(
            grade,
            false,
            horaInicial,
            horaFinal,
            grade ? Cabecalhos(deDomingoASabado) : [],
            dias);
    }

    private static DiaDeCalendario MontarDia(
        DateTime dia,
        DateTime inicio,
        DateTime fim,
        DateTime hoje,
        DateTimeOffset agora,
        List<Fatia> fatias)
    {
        var blocos = Juntar(fatias);
        var minutos = 0;
        foreach (var bloco in blocos)
            minutos += bloco.FimMinuto - bloco.InicioMinuto;

        return new DiaDeCalendario(
            dia,
            Rotulo(dia, agora),
            dia.Day,
            dia >= inicio && dia <= fim,
            dia == hoje,
            TextoDeDuracao.De(TimeSpan.FromMinutes(minutos)),
            blocos,
            LinhasDe(blocos));
    }

    private static List<BlocoDeCalendario> Juntar(List<Fatia> fatias)
    {
        var porChave = new Dictionary<string, List<Fatia>>(StringComparer.Ordinal);
        foreach (var fatia in fatias)
        {
            if (!porChave.TryGetValue(fatia.Descricao.Chave, out var lista))
            {
                lista = [];
                porChave[fatia.Descricao.Chave] = lista;
            }

            lista.Add(fatia);
        }

        var soltos = new List<(int Inicio, int Fim, int InicioSeg, int FimSeg, string Titulo, string Subtitulo, string Tipo, string Chave, AlvoDeVinculo Alvo)>();
        foreach (var (chave, lista) in porChave)
        {
            lista.Sort((a, b) => a.Inicio.CompareTo(b.Inicio));
            var inicio = lista[0].Inicio;
            var fim = lista[0].Fim;
            var inicioSeg = lista[0].InicioSeg;
            var fimSeg = lista[0].FimSeg;
            var titulo = lista[0].Descricao.Titulo;
            var votos = Voto(lista[0]);
            var tipos = TipoVoto(lista[0]);
            var alvo = new AlvoEmFaixa(lista[0]);
            for (var i = 1; i < lista.Count; i++)
            {
                var atual = lista[i];
                if (atual.Inicio <= fim + FolgaMinutos)
                {
                    if (atual.Fim > fim)
                        fim = atual.Fim;
                    if (atual.FimSeg > fimSeg)
                        fimSeg = atual.FimSeg;
                    Somar(votos, atual);
                    SomarTipo(tipos, atual);
                    alvo.Juntar(atual);
                    continue;
                }

                soltos.Add((inicio, fim, inicioSeg, fimSeg, titulo, SubtituloDe(votos), TipoDe(tipos), chave, alvo.Concluir()));
                inicio = atual.Inicio;
                fim = atual.Fim;
                inicioSeg = atual.InicioSeg;
                fimSeg = atual.FimSeg;
                titulo = atual.Descricao.Titulo;
                votos = Voto(atual);
                tipos = TipoVoto(atual);
                alvo = new AlvoEmFaixa(atual);
            }

            soltos.Add((inicio, fim, inicioSeg, fimSeg, titulo, SubtituloDe(votos), TipoDe(tipos), chave, alvo.Concluir()));
        }

        soltos.Sort((a, b) =>
        {
            var comparacao = a.Inicio.CompareTo(b.Inicio);
            return comparacao != 0 ? comparacao : (b.Fim - b.Inicio).CompareTo(a.Fim - a.Inicio);
        });

        var ocupacao = new List<int>();
        var blocos = new List<BlocoDeCalendario>(soltos.Count);
        foreach (var item in soltos)
        {
            var trilha = 0;
            while (trilha < ocupacao.Count && ocupacao[trilha] > item.Inicio)
                trilha++;
            if (trilha == ocupacao.Count)
                ocupacao.Add(item.Fim);
            else
                ocupacao[trilha] = item.Fim;

            blocos.Add(new BlocoDeCalendario(
                item.Inicio,
                item.Fim,
                item.InicioSeg,
                item.FimSeg,
                trilha,
                item.Titulo,
                item.Subtitulo,
                item.Tipo,
                item.Chave,
                Dica(item.Tipo, item.Titulo, item.Subtitulo, item.Inicio, item.Fim),
                item.Alvo));
        }

        return blocos;
    }

    private static Dictionary<string, int> Voto(Fatia fatia)
    {
        var votos = new Dictionary<string, int>(StringComparer.Ordinal);
        Somar(votos, fatia);
        return votos;
    }

    private static void Somar(Dictionary<string, int> votos, Fatia fatia)
    {
        var peso = Math.Max(1, fatia.Fim - fatia.Inicio);
        votos[fatia.Descricao.Subtitulo] = votos.GetValueOrDefault(fatia.Descricao.Subtitulo) + peso;
    }

    private static Dictionary<string, int> TipoVoto(Fatia fatia)
    {
        var votos = new Dictionary<string, int>(StringComparer.Ordinal);
        SomarTipo(votos, fatia);
        return votos;
    }

    private static void SomarTipo(Dictionary<string, int> votos, Fatia fatia)
    {
        if (fatia.Descricao.Tipo.Length == 0)
            return;

        var peso = Math.Max(1, fatia.Fim - fatia.Inicio);
        votos[fatia.Descricao.Tipo] = votos.GetValueOrDefault(fatia.Descricao.Tipo) + peso;
    }

    private static string TipoDe(Dictionary<string, int> votos) =>
        votos.Count == 0 ? "" : SubtituloDe(votos);

    private static string SubtituloDe(Dictionary<string, int> votos)
    {
        var ordenados = votos
            .OrderByDescending(par => par.Value)
            .ThenBy(par => par.Key, StringComparer.Ordinal)
            .Take(2)
            .ToList();
        if (ordenados.Count < 2 || ordenados[1].Value * 2 < ordenados[0].Value)
            return ordenados[0].Key;

        return ordenados[0].Key + " · " + ordenados[1].Key;
    }

    private static IReadOnlyList<LinhaDoDia> LinhasDe(IReadOnlyList<BlocoDeCalendario> blocos)
    {
        return blocos
            .GroupBy(bloco => bloco.Chave)
            .Select(grupo =>
            {
                var minutos = grupo.Sum(bloco => bloco.FimMinuto - bloco.InicioMinuto);
                return new LinhaDoDia(
                    grupo.First().Titulo,
                    TextoDeDuracao.De(TimeSpan.FromMinutes(minutos)),
                    grupo.Key,
                    minutos);
            })
            .OrderByDescending(linha => linha.Minutos)
            .ThenBy(linha => linha.Titulo, StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .ToList();
    }

    private static bool TentarDescrever(
        TrechoDeEsforco trecho,
        ModoDoCalendario modo,
        NomesDePainel nomes,
        IReadOnlyList<OcultacaoDeEscritorio> ocultos,
        IReadOnlyDictionary<long, string>? enderecos,
        out Descricao descricao)
    {
        var intervalo = trecho.Intervalo;
        var origem = new Origem(
            intervalo.Programa,
            intervalo.Titulo,
            ValidadeDeVinculo.Endereco(intervalo, enderecos),
            ResolvedorDeEscritorio.Descoberto(intervalo, nomes));
        var tipo = Classificar(intervalo);
        if (trecho.Vinculo is not null && tipo.Length == 0)
            tipo = TipoVinculo;

        if (modo == ModoDoCalendario.Programas)
        {
            var tela = Tela(intervalo, nomes);
            descricao = new Descricao(intervalo.Programa, tela, "programa:" + intervalo.Programa, tipo, origem);
            return intervalo.Programa.Length > 0;
        }

        if (modo == ModoDoCalendario.Telas)
        {
            var tela = Tela(intervalo, nomes);
            descricao = new Descricao(tela, intervalo.Programa, "tela:" + NomesDePainel.Normalizar(tela), tipo, origem);
            return tela.Length > 0;
        }

        if (trecho.Vinculo is VinculoAplicado vinculo)
        {
            var rotulo = nomes.RotuloDeEscritorio(vinculo.Escritorio);
            if (nomes.Oculto(vinculo.Escritorio, rotulo, ocultos))
            {
                descricao = default;
                return false;
            }

            var feito = Feito(intervalo, nomes);
            descricao = new Descricao(
                rotulo,
                vinculo.Categoria.Length == 0 ? feito : $"{vinculo.Categoria} · {feito}",
                "id:" + vinculo.Escritorio,
                TipoVinculo,
                origem);
            return true;
        }

        if (!TentarEscritorio(intervalo, nomes, out var id, out var titulo))
        {
            descricao = default;
            return false;
        }

        if (nomes.Oculto(id, titulo, ocultos))
        {
            descricao = default;
            return false;
        }

        var chave = string.IsNullOrEmpty(id)
            ? "nome:" + NomesDePainel.Normalizar(titulo)
            : "id:" + id;
        descricao = new Descricao(titulo, Feito(intervalo, nomes), chave, tipo, origem);
        return true;
    }

    private static bool TentarEscritorio(
        IntervaloDeEsforco intervalo,
        NomesDePainel nomes,
        out string? id,
        out string titulo)
    {
        id = null;
        titulo = "";
        var reuniao = InterpretadorDeReuniao.Ler(intervalo.Titulo);

        if (intervalo.EhPainelSplitC || intervalo.EhChatwoot)
        {
            var bruto = intervalo.Escritorio.Trim();
            if (bruto.Length == 0)
                return false;

            id = bruto;
            titulo = nomes.RotuloDeEscritorio(bruto);
            return true;
        }

        if (reuniao is not null)
        {
            id = nomes.IdDaReuniao(reuniao.Nome);
            titulo = id is null ? reuniao.Nome : nomes.RotuloDeEscritorio(id);
            return titulo.Length > 0;
        }

        var campo = intervalo.Escritorio.Trim();
        if (campo.Length == 0)
            return false;

        if (nomes.Conhece(campo))
        {
            id = campo;
            titulo = nomes.RotuloDeEscritorio(campo);
            return true;
        }

        id = nomes.IdDaReuniao(campo) ?? nomes.IdNoTexto(campo);
        titulo = id is null ? campo : nomes.RotuloDeEscritorio(id);
        return titulo.Length > 0;
    }

    private static string Classificar(IntervaloDeEsforco intervalo)
    {
        if (intervalo.EhChatwoot)
            return "Chatwoot";
        if (InterpretadorDeReuniao.Ler(intervalo.Titulo) is not null)
            return "Reunião";
        if (intervalo.EhPainelSplitC)
            return "Painel";
        return "";
    }

    private static string Feito(IntervaloDeEsforco intervalo, NomesDePainel nomes) =>
        Tela(intervalo, nomes);

    private static string Tela(IntervaloDeEsforco intervalo, NomesDePainel nomes)
    {
        var reuniao = InterpretadorDeReuniao.Ler(intervalo.Titulo);
        if (reuniao is not null)
            return reuniao.Tema == "Sem tema" ? "Reunião" : reuniao.Tema;
        if (intervalo.EhChatwoot)
            return "Chatwoot";
        if (intervalo.EhPainelSplitC)
        {
            var sessao = intervalo.Sessao.Trim();
            return sessao.Length > 0 ? sessao : "Painel";
        }

        var contexto = NomesDePainel.Exibir(intervalo.Contexto);
        if (contexto.Length > 0)
            return contexto;

        var extraido = ExtratorDeContexto.Extrair(intervalo.Titulo);
        return extraido.Length > 0 ? extraido : "Sem título";
    }

    private static IEnumerable<Fatia> Fatiar(
        IntervaloDeEsforco intervalo,
        DateTime inicioPeriodo,
        DateTime fimPeriodo,
        DateTimeOffset agora,
        Descricao descricao)
    {
        var comeco = intervalo.Inicio.ToLocalTime().DateTime;
        var termino = (intervalo.Fim ?? agora).ToLocalTime().DateTime;
        if (termino < comeco)
            termino = comeco;
        if (termino == comeco)
            termino = comeco.AddMinutes(1);

        var limiteInicio = inicioPeriodo.Date;
        var limiteFim = fimPeriodo.Date.AddDays(1);
        if (termino <= limiteInicio || comeco >= limiteFim)
            yield break;
        if (comeco < limiteInicio)
            comeco = limiteInicio;
        if (termino > limiteFim)
            termino = limiteFim;

        for (var dia = comeco.Date; dia < termino; dia = dia.AddDays(1))
        {
            var fatiaInicio = comeco > dia ? comeco : dia;
            var proximo = dia.AddDays(1);
            var fatiaFim = termino < proximo ? termino : proximo;
            if (fatiaFim <= fatiaInicio)
                continue;

            var (inicio, fim) = Minutos(fatiaInicio, fatiaFim, dia);
            var (inicioSeg, fimSeg) = Segundos(fatiaInicio, fatiaFim, dia);
            if (fimSeg < inicioSeg)
                continue;

            var comecoReal = intervalo.Inicio;
            var meiaNoite = new DateTimeOffset(dia);
            if (comecoReal < meiaNoite)
                comecoReal = meiaNoite;
            var fimReal = intervalo.Fim ?? agora;
            var virada = new DateTimeOffset(proximo);
            if (fimReal > virada)
                fimReal = virada;
            if (fimReal < comecoReal)
                fimReal = comecoReal;

            yield return new Fatia(dia, inicio, fim, inicioSeg, fimSeg, descricao, comecoReal, fimReal);
        }
    }

    private static (int Inicio, int Fim) Minutos(DateTime comeco, DateTime termino, DateTime dia)
    {
        var inicio = comeco <= dia ? 0 : comeco.Hour * 60 + comeco.Minute;
        if (termino >= dia.AddDays(1))
            return (inicio, 24 * 60);

        var fim = termino.Hour * 60 + termino.Minute;
        if (fim <= inicio)
            fim = inicio + 1;
        if (fim > 24 * 60)
            fim = 24 * 60;
        return (inicio, fim);
    }

    private static (int Inicio, int Fim) Segundos(DateTime comeco, DateTime termino, DateTime dia)
    {
        var inicio = comeco <= dia ? 0 : SegundosDoDia(comeco, dia);
        if (termino >= dia.AddDays(1))
            return (inicio, 24 * 60 * 60);

        var fim = SegundosDoDia(termino, dia);
        if (fim > 24 * 60 * 60)
            fim = 24 * 60 * 60;
        return (inicio, fim);
    }

    private static int SegundosDoDia(DateTime instante, DateTime dia)
    {
        var ticks = (instante - dia).Ticks;
        if (ticks <= 0)
            return 0;
        return (int)(ticks / TimeSpan.TicksPerSecond);
    }

    private static (int Inicio, int Fim) Janela(IReadOnlyList<DiaDeCalendario> dias)
    {
        var minimo = int.MaxValue;
        var maximo = int.MinValue;
        foreach (var dia in dias)
        {
            foreach (var bloco in dia.Blocos)
            {
                if (bloco.InicioMinuto < minimo)
                    minimo = bloco.InicioMinuto;
                if (bloco.FimMinuto > maximo)
                    maximo = bloco.FimMinuto;
            }
        }

        if (minimo == int.MaxValue)
            return (8, 18);

        var inicio = Math.Min(8, minimo / 60);
        var fim = maximo % 60 == 0 ? maximo / 60 : (maximo + 59) / 60;
        fim = Math.Max(18, fim);
        if (fim <= inicio)
            fim = inicio + 1;
        if (fim > 24)
            fim = 24;
        return (inicio, fim);
    }

    private static List<DateTime> Enumerar(DateTime inicio, DateTime fim)
    {
        var dias = new List<DateTime>();
        for (var dia = inicio; dia <= fim; dia = dia.AddDays(1))
            dias.Add(dia);
        return dias;
    }

    private static List<DateTime> EnumerarGrade(DateTime inicio, DateTime fim, bool deDomingoASabado)
    {
        var primeiro = InicioDaSemana(inicio, deDomingoASabado);
        var ultimo = InicioDaSemana(fim, deDomingoASabado).AddDays(6);
        return Enumerar(primeiro, ultimo);
    }

    private static DateTime InicioDaSemana(DateTime dia, bool deDomingoASabado)
    {
        if (deDomingoASabado)
            return dia.AddDays(-(int)dia.DayOfWeek);

        return dia.AddDays(-((int)dia.DayOfWeek + 6) % 7);
    }

    private static IReadOnlyList<string> Cabecalhos(bool deDomingoASabado) =>
        deDomingoASabado
            ? ["dom.", "seg.", "ter.", "qua.", "qui.", "sex.", "sáb."]
            : ["seg.", "ter.", "qua.", "qui.", "sex.", "sáb.", "dom."];

    private static string Rotulo(DateTime dia, DateTimeOffset agora)
    {
        var instante = new DateTimeOffset(dia.Year, dia.Month, dia.Day, 12, 0, 0, agora.Offset);
        return RotuloDeDia.De(instante, agora);
    }

    private static string Dica(string tipo, string titulo, string subtitulo, int inicio, int fim)
    {
        var duracao = TextoDeDuracao.De(TimeSpan.FromMinutes(Math.Max(0, fim - inicio)));
        var cabeca = tipo.Length == 0 ? "" : tipo + "\n";
        return $"{cabeca}{titulo}\n{subtitulo}\n{Hora(inicio)} – {Hora(fim)} · {duracao}";
    }

    private static string Hora(int minuto)
    {
        if (minuto >= 24 * 60)
            return "24:00";
        if (minuto < 0)
            minuto = 0;
        return $"{minuto / 60:00}:{minuto % 60:00}";
    }

    public const string TipoVinculo = "Vínculo";

    private readonly record struct Origem(string Programa, string Titulo, string Endereco, bool Descoberto);

    private readonly record struct Descricao(string Titulo, string Subtitulo, string Chave, string Tipo, Origem Origem);

    private readonly record struct Fatia(
        DateTime Dia,
        int Inicio,
        int Fim,
        int InicioSeg,
        int FimSeg,
        Descricao Descricao,
        DateTimeOffset InicioReal,
        DateTimeOffset FimReal);

    /// <summary>
    /// Junta as fatias de uma faixa fundida: o período inteiro e a janela que mais durou,
    /// de preferência uma sem escritório descoberto, que é onde o vínculo manual tem efeito.
    /// </summary>
    private sealed class AlvoEmFaixa
    {
        private DateTimeOffset _inicio;
        private DateTimeOffset _fim;
        private Origem _origem;
        private TimeSpan _peso;

        public AlvoEmFaixa(Fatia fatia)
        {
            _inicio = fatia.InicioReal;
            _fim = fatia.FimReal;
            _origem = fatia.Descricao.Origem;
            _peso = fatia.FimReal - fatia.InicioReal;
        }

        public void Juntar(Fatia fatia)
        {
            if (fatia.InicioReal < _inicio)
                _inicio = fatia.InicioReal;
            if (fatia.FimReal > _fim)
                _fim = fatia.FimReal;

            var origem = fatia.Descricao.Origem;
            var peso = fatia.FimReal - fatia.InicioReal;
            var melhor = origem.Descoberto != _origem.Descoberto
                ? !origem.Descoberto
                : peso > _peso;
            if (!melhor)
                return;

            _origem = origem;
            _peso = peso;
        }

        public AlvoDeVinculo Concluir() =>
            new(_origem.Programa, _origem.Titulo, _origem.Endereco, _inicio, _fim, _origem.Descoberto);
    }
}
