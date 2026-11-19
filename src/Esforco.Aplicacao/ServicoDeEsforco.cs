using Esforco.Dominio;

namespace Esforco.Aplicacao;

public sealed partial class ServicoDeEsforco
{
    private readonly ILeitorDeJanelaDaFrente _leitor;
    private readonly IRepositorioDeIntervalos _repositorio;
    private readonly IFonteDeRegrasDeEscritorio _regras;
    private readonly IRelogio _relogio;
    private readonly ServicoDeVinculos? _vinculos;
    private readonly object _gate = new();
    public static readonly TimeSpan FolgaDeAusencia = TimeSpan.FromSeconds(15);

    private DateTimeOffset? _ultimaVistaDaReuniao;
    private DateTimeOffset? _ultimoPulso;
    private Dictionary<string, PreferenciaDePrograma>? _preferencias;
    private int _ocupado;
    private bool _emLeitura;
    private bool _suspenso;
    private bool _proximaReuniaoRetomada;

    public ServicoDeEsforco(
        ILeitorDeJanelaDaFrente leitor,
        IRepositorioDeIntervalos repositorio,
        IFonteDeRegrasDeEscritorio regras,
        IRelogio relogio,
        ServicoDeVinculos? vinculos = null)
    {
        _leitor = leitor;
        _repositorio = repositorio;
        _regras = regras;
        _relogio = relogio;
        _vinculos = vinculos;
    }

    public bool EmLeitura
    {
        get
        {
            lock (_gate)
                return _emLeitura;
        }
    }

    public void Iniciar()
    {
        lock (_gate)
        {
            if (_emLeitura)
                return;
            _emLeitura = true;
        }

        Pulso();
    }

    public void Parar()
    {
        lock (_gate)
        {
            if (!_emLeitura)
                return;
            _emLeitura = false;
            var agora = _relogio.Agora();
            FecharAberto(agora);
            FecharReuniaoEmSegundoPlano(agora);
            _ultimoPulso = null;
        }
    }

    public void Ausentar()
    {
        lock (_gate)
        {
            var agora = _relogio.Agora();
            FecharAberto(agora);
            FecharReuniaoEmSegundoPlano(agora);
            _suspenso = true;
            _proximaReuniaoRetomada = true;
            _ultimoPulso = agora;
        }
    }

    public void Retomar()
    {
        lock (_gate)
        {
            _suspenso = false;
            _ultimoPulso = _relogio.Agora();
        }
    }

    public void Pulso()
    {
        if (!EmLeitura)
            return;

        if (Interlocked.CompareExchange(ref _ocupado, 1, 0) != 0)
            return;

        try
        {
            lock (_gate)
            {
                if (!_emLeitura || _suspenso)
                    return;

                CortarSeHouveAusencia(_relogio.Agora());
            }

            AmostraDeJanela? amostra;
            try
            {
                amostra = _leitor.Ler();
            }
            catch (Exception)
            {
                return;
            }

            AmostraDeJanela? reuniaoMinimizada = null;
            try
            {
                reuniaoMinimizada = _leitor.LerReuniaoMinimizada();
            }
            catch (Exception)
            {
                reuniaoMinimizada = null;
            }

            lock (_gate)
            {
                if (!_emLeitura || _suspenso || amostra is null)
                    return;

                if (EhJanelinha(amostra))
                {
                    if (_segura is null)
                        return;
                    amostra = _segura;
                }
                else
                {
                    _ultimaAmostra = amostra;
                }

                var agora = _relogio.Agora();
                CortarSeHouveAusencia(agora);
                var titulo = (amostra.Titulo ?? "").Trim();
                var endereco = (amostra.Endereco ?? "").Trim();
                var conversa = (amostra.Conversa ?? "").Trim();
                if (conversa.Length > 200)
                    conversa = conversa[..200];
                var local = InterpretadorDePainelSplitC.Interpretar(endereco);
                var nome = NomeDoPrograma(amostra.Programa, local, titulo, endereco);
                if (EstaPausado(nome))
                {
                    FecharAberto(agora);
                    var fundo = _repositorio.ObterReuniaoEmSegundoPlano();
                    if (fundo is not null && string.Equals(fundo.Programa, nome, StringComparison.Ordinal))
                        Fechar(fundo, agora);
                    return;
                }

                var reuniao = ReuniaoAberta();
                var viu = ViuReuniao(reuniao, titulo, reuniaoMinimizada);
                if (reuniao is not null && !viu)
                {
                    var vista = _ultimaVistaDaReuniao ?? reuniao.Inicio;
                    var outra = OutraReuniao(reuniao, titulo);
                    if (outra || agora - vista > ContinuidadeDeReuniao.Folga)
                    {
                        Fechar(reuniao, outra ? agora : vista);
                        reuniao = null;
                        if (!outra)
                            _ultimaVistaDaReuniao = null;
                    }
                }

                if (reuniao is not null && viu)
                {
                    _ultimaVistaDaReuniao = agora;
                    if (reuniao.Id is long idVisto)
                        _repositorio.MarcarVisto(idVisto, agora);
                }

                if (reuniao is not null && FrenteMantemReuniao(reuniao, titulo))
                {
                    if (reuniao.EmSegundoPlano)
                        _repositorio.DefinirSegundoPlano(reuniao, false);

                    var outro = _repositorio.ObterAberto();
                    if (outro is not null && outro.Id != reuniao.Id)
                        Fechar(outro, agora);
                    return;
                }

                if (reuniao is not null && !reuniao.EmSegundoPlano)
                    _repositorio.DefinirSegundoPlano(reuniao, true);

                var aberto = _repositorio.ObterAberto();
                if (aberto is not null && MesmaAtividade(aberto, nome, titulo, endereco, conversa, amostra.Minimizada, local))
                {
                    if (aberto.Id is long id)
                        _repositorio.MarcarVisto(id, agora);
                    return;
                }

                if (aberto is not null)
                    FecharAberto(agora);

                var novo = local is null
                    ? IntervaloDeEsforco.Abrir(
                        nome,
                        titulo,
                        Contexto(titulo, endereco, conversa),
                        Escritorio(titulo, endereco, conversa),
                        amostra.Minimizada,
                        agora)
                    : IntervaloDeEsforco.AbrirPainel(titulo, local, amostra.Minimizada, agora);
                if (_proximaReuniaoRetomada && InterpretadorDeReuniao.Ler(titulo) is not null)
                {
                    novo.MarcarRetomado();
                    _proximaReuniaoRetomada = false;
                }

                _repositorio.Adicionar(novo);
                if (local is null && endereco.Length > 0)
                    _vinculos?.GuardarEnderecoSeVinculado(novo, endereco);
                if (InterpretadorDeReuniao.Ler(titulo) is not null)
                    _ultimaVistaDaReuniao = agora;
            }
        }
        finally
        {
            Interlocked.Exchange(ref _ocupado, 0);
        }
    }

    public void AlternarOculto(string programa) =>
        Alternar(programa, preferencia => preferencia with { Oculto = !preferencia.Oculto });

    public void AlternarFixado(string programa) =>
        Alternar(programa, preferencia => preferencia with { Fixado = !preferencia.Fixado });

    public void AlternarPausado(string programa) =>
        Alternar(programa, preferencia => preferencia with { Pausado = !preferencia.Pausado });

    public void ExcluirPrograma(string programa)
    {
        var nome = (programa ?? "").Trim();
        if (nome.Length == 0)
            return;

        lock (_gate)
        {
            _repositorio.ExcluirPrograma(nome);
            _preferencias = null;
            LimparEnderecosSemIntervalo();
        }
    }

    public void ZerarPeriodo(DateTimeOffset inicio, DateTimeOffset fim)
    {
        lock (_gate)
        {
            var plano = CorteDePeriodo.Planejar(_repositorio.Listar(), inicio, fim, _relogio.Agora());
            foreach (var id in plano.Excluir)
                _repositorio.ExcluirIntervalo(id);
            foreach (var intervalo in plano.Atualizar)
                _repositorio.AtualizarLimites(intervalo);
            foreach (var intervalo in plano.Criar)
            {
                _repositorio.Adicionar(intervalo);
                if (intervalo.Fim is not null)
                    _repositorio.SalvarFechamento(intervalo);
            }

            LimparEnderecosSemIntervalo();
        }
    }

    public ResumoDeEsforco ConsultarResumo(DateTime? diaInicial = null, DateTime? diaFinal = null)
    {
        var gravados = _repositorio.Listar();
        var unidos = ContinuidadeDeReuniao.Unir(gravados);
        var agora = _relogio.Agora();
        var lista = FiltrarDias(unidos, diaInicial, diaFinal);
        var nomes = new NomesDePainel(_repositorio.ListarCadastros());
        var trechos = AplicadorDeVinculos.Aplicar(lista, _vinculos?.Contexto(), nomes, agora, unidos);
        PainelDuranteReuniao.Marcar(lista, nomes, agora, trechos);
        var grupos = AgrupadorDeEsforco.Agrupar(lista, agora, nomes);
        IReadOnlyDictionary<string, PreferenciaDePrograma> preferencias;
        lock (_gate)
            preferencias = new Dictionary<string, PreferenciaDePrograma>(Mapa(), StringComparer.Ordinal);

        var ordenados = grupos
            .Select(dia => dia with
            {
                Filhos = dia.Filhos
                    .OrderByDescending(no => preferencias.TryGetValue(no.Rotulo, out var preferencia) && preferencia.Fixado)
                    .ToList(),
            })
            .ToList();
        return new ResumoDeEsforco(EmLeitura, ordenados, preferencias, gravados.Count > 0);
    }

    public QuadroDeCalendario ConsultarCalendario(
        DateTime diaInicial,
        DateTime diaFinal,
        ModoDoCalendario modo,
        bool emGrade,
        bool deDomingoASabado)
    {
        var agora = _relogio.Agora();
        var unidos = ContinuidadeDeReuniao.Unir(_repositorio.Listar());
        var nomes = new NomesDePainel(_repositorio.ListarCadastros());
        return CalendarioDeEsforco.Montar(
            unidos,
            diaInicial,
            diaFinal,
            agora,
            modo,
            nomes,
            emGrade,
            deDomingoASabado,
            _repositorio.ListarOcultacoes(),
            _vinculos?.Contexto());
    }

    public IReadOnlyList<LinhaDeEscritorio> ConsultarEscritorios(DateTime? diaInicial = null, DateTime? diaFinal = null)
    {
        var agora = _relogio.Agora();
        var unidos = ContinuidadeDeReuniao.Unir(_repositorio.Listar());
        var lista = FiltrarDias(unidos, diaInicial, diaFinal);
        var linhas = RelatorioDeEscritorios.Montar(lista, agora, _repositorio.ListarCadastros(), unidos, _vinculos?.Contexto());
        return ListaNegraDeEscritorios.Visiveis(linhas, _repositorio.ListarOcultacoes());
    }

    public IReadOnlyList<OcultacaoDeEscritorio> ListarOcultacoes() => _repositorio.ListarOcultacoes();

    public string? OcultarNoRelatorio(bool porId, string? texto)
    {
        lock (_gate)
        {
            var aviso = ListaNegraDeEscritorios.Impedir(porId, texto, _repositorio.ListarOcultacoes());
            if (aviso is not null)
                return aviso;

            var limpo = porId ? (texto ?? "").Trim() : NomesDePainel.Exibir(texto);
            _repositorio.AdicionarOcultacao(new OcultacaoDeEscritorio(porId, limpo));
            return null;
        }
    }

    public void ReexibirNoRelatorio(OcultacaoDeEscritorio ocultacao)
    {
        ArgumentNullException.ThrowIfNull(ocultacao);
        lock (_gate)
            _repositorio.RemoverOcultacao(ocultacao);
    }

    public void DefinirNomeDoEscritorio(string id, string? nome)
    {
        var chave = (id ?? "").Trim();
        if (chave.Length == 0)
            return;

        lock (_gate)
            _repositorio.DefinirNomeDeEscritorio(chave, NomesDePainel.Exibir(nome));
    }

    public void DefinirNomeDoPlano(string escritorio, string plano, string? nome)
    {
        var id = (escritorio ?? "").Trim();
        var codigo = (plano ?? "").Trim();
        if (id.Length == 0 || codigo.Length == 0)
            return;

        lock (_gate)
            _repositorio.DefinirNomeDePlano(id, codigo, NomesDePainel.Exibir(nome));
    }

    public string? AdicionarVariacao(string escritorio, string? texto)
    {
        var id = (escritorio ?? "").Trim();
        if (id.Length == 0)
            return "Este escritório não tem id.";

        lock (_gate)
        {
            var cadastro = _repositorio.ListarCadastros().FirstOrDefault(item => item.Id == id);
            var aviso = VariacaoDeEscritorio.Impedir(texto, cadastro?.Nome ?? "", cadastro?.Variacoes ?? []);
            if (aviso is not null)
                return aviso;

            _repositorio.AdicionarVariacao(id, NomesDePainel.Exibir(texto));
            return null;
        }
    }

    public void RemoverVariacao(string escritorio, string variacao)
    {
        var id = (escritorio ?? "").Trim();
        var texto = variacao ?? "";
        if (id.Length == 0 || texto.Length == 0)
            return;

        lock (_gate)
            _repositorio.RemoverVariacao(id, texto);
    }

    private static IReadOnlyList<IntervaloDeEsforco> FiltrarDias(
        IReadOnlyList<IntervaloDeEsforco> intervalos,
        DateTime? diaInicial,
        DateTime? diaFinal)
    {
        if (diaInicial is not DateTime inicio || diaFinal is not DateTime fim)
            return intervalos;

        var de = inicio.Date;
        var ate = fim.Date;
        if (ate < de)
            (de, ate) = (ate, de);

        return intervalos
            .Where(intervalo =>
            {
                var dia = intervalo.Inicio.ToLocalTime().Date;
                return dia >= de && dia <= ate;
            })
            .ToList();
    }

    private void Alternar(string programa, Func<PreferenciaDePrograma, PreferenciaDePrograma> mudar)
    {
        var nome = (programa ?? "").Trim();
        if (nome.Length == 0)
            return;

        lock (_gate)
        {
            var atual = Mapa().TryGetValue(nome, out var guardada)
                ? guardada
                : PreferenciaDePrograma.Vazia(nome);
            var nova = mudar(atual);
            _repositorio.SalvarPreferencia(nova);
            _preferencias = null;
            if (nova.Pausado)
                FecharSeFor(nome);
        }
    }

    private void FecharSeFor(string programa)
    {
        var aberto = _repositorio.ObterAberto();
        var agora = _relogio.Agora();
        if (aberto is not null && string.Equals(aberto.Programa, programa, StringComparison.Ordinal))
            FecharAberto(agora);

        var fundo = _repositorio.ObterReuniaoEmSegundoPlano();
        if (fundo is not null && string.Equals(fundo.Programa, programa, StringComparison.Ordinal))
            Fechar(fundo, agora);
    }

    private bool EstaPausado(string programa) =>
        Mapa().TryGetValue(programa, out var preferencia) && preferencia.Pausado;

    private Dictionary<string, PreferenciaDePrograma> Mapa()
    {
        if (_preferencias is not null)
            return _preferencias;

        _preferencias = _repositorio.ListarPreferencias()
            .ToDictionary(preferencia => preferencia.Programa, StringComparer.Ordinal);
        return _preferencias;
    }

    private static string NomeDoPrograma(string? programa, LocalDoPainel? local, string titulo, string endereco)
    {
        if (local is not null)
            return InterpretadorDePainelSplitC.NomeDoPrograma;

        if (InterpretadorDeChatwoot.EhAba(titulo, endereco))
            return InterpretadorDeChatwoot.NomeDoPrograma;

        var nome = (programa ?? "").Trim();
        return nome.Length == 0 ? "Programa desconhecido" : nome;
    }

    private string Contexto(string titulo, string endereco, string conversa) =>
        InterpretadorDeChatwoot.EhAba(titulo, endereco) && conversa.Length > 0
            ? conversa
            : ExtratorDeContexto.Extrair(titulo);

    private string Escritorio(string titulo, string endereco, string conversa)
    {
        if (InterpretadorDeChatwoot.EhAba(titulo, endereco) && conversa.Length > 0)
            return new NomesDePainel(_repositorio.ListarCadastros()).IdNoTexto(conversa) ?? "";

        return ResolvedorDeEscritorio.Resolver(titulo, _regras.Carregar());
    }

    private static bool MesmaAtividade(
        IntervaloDeEsforco aberto,
        string programa,
        string titulo,
        string endereco,
        string conversa,
        bool minimizada,
        LocalDoPainel? local)
    {
        if (aberto.Fim is not null)
            return false;

        if (InterpretadorDeReuniao.Mesma(aberto.Titulo, titulo))
            return true;

        if (aberto.Minimizada != minimizada)
            return false;

        if (aberto.EhPainelSplitC)
        {
            if (local is not null)
                return aberto.MesmoLocal(local);

            return endereco.Length == 0
                && string.Equals(aberto.Titulo, titulo, StringComparison.Ordinal);
        }

        if (local is not null)
            return false;

        if (InterpretadorDeChatwoot.EhAba(aberto.Titulo, null) || InterpretadorDeChatwoot.EhAba(titulo, endereco))
        {
            if (!InterpretadorDeChatwoot.EhAba(aberto.Titulo, null) || !InterpretadorDeChatwoot.EhAba(titulo, endereco))
                return false;

            if (!string.Equals(aberto.Programa, (programa ?? "").Trim(), StringComparison.Ordinal))
                return false;

            if (conversa.Length == 0)
                return !InterpretadorDeChatwoot.SaiuDaConversa(endereco);

            return string.Equals(aberto.Contexto, conversa, StringComparison.Ordinal);
        }

        return aberto.Continua(programa, titulo, minimizada);
    }

    private IntervaloDeEsforco? ReuniaoAberta()
    {
        var fundo = _repositorio.ObterReuniaoEmSegundoPlano();
        if (fundo is not null && InterpretadorDeReuniao.Ler(fundo.Titulo) is not null)
            return fundo;

        var aberto = _repositorio.ObterAberto();
        if (aberto is not null && InterpretadorDeReuniao.Ler(aberto.Titulo) is not null)
            return aberto;

        return null;
    }

    private static bool ViuReuniao(IntervaloDeEsforco? reuniao, string titulo, AmostraDeJanela? minimizada)
    {
        if (reuniao is null)
            return false;

        if (InterpretadorDeReuniao.Mesma(reuniao.Titulo, titulo))
            return true;

        if (minimizada is not null && InterpretadorDeReuniao.Mesma(reuniao.Titulo, minimizada.Titulo))
            return true;

        return InterpretadorDeReuniao.PareceJanelaDoMeet(titulo) && !OutraReuniao(reuniao, titulo);
    }

    private static bool FrenteMantemReuniao(IntervaloDeEsforco reuniao, string titulo) =>
        InterpretadorDeReuniao.Mesma(reuniao.Titulo, titulo)
        || (InterpretadorDeReuniao.PareceJanelaDoMeet(titulo) && !OutraReuniao(reuniao, titulo));

    private static bool OutraReuniao(IntervaloDeEsforco reuniao, string titulo) =>
        InterpretadorDeReuniao.Ler(titulo) is not null
        && !InterpretadorDeReuniao.Mesma(reuniao.Titulo, titulo);

    private void CortarSeHouveAusencia(DateTimeOffset agora)
    {
        if (_ultimoPulso is DateTimeOffset anterior && agora - anterior > FolgaDeAusencia)
        {
            FecharAberto(anterior);
            FecharReuniaoEmSegundoPlano(anterior);
            _proximaReuniaoRetomada = true;
        }

        _ultimoPulso = agora;
    }

    private void FecharAberto(DateTimeOffset agora)
    {
        var aberto = _repositorio.ObterAberto();
        if (aberto is null)
            return;

        Fechar(aberto, agora);
    }

    private void FecharReuniaoEmSegundoPlano(DateTimeOffset agora)
    {
        var fundo = _repositorio.ObterReuniaoEmSegundoPlano();
        if (fundo is not null)
            Fechar(fundo, agora);
    }

    private void Fechar(IntervaloDeEsforco intervalo, DateTimeOffset agora)
    {
        intervalo.Fechar(agora);
        _repositorio.SalvarFechamento(intervalo);
    }
}
