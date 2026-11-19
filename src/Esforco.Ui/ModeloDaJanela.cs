using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Esforco.Aplicacao;
using Esforco.Dominio;

namespace Esforco.Ui;

public sealed class ModeloDaJanela : INotifyPropertyChanged
{
    private readonly ServicoDeEsforco _servico;
    private readonly ServicoDeVinculos _vinculos;
    private readonly AtalhoDaJanela _atalho;
    private JanelaDeVinculo? _janelinha;
    private bool _abrindoVinculo;
    private readonly Dictionary<string, bool> _expansao = new();
    private bool? _expansaoPadrao;
    private bool _emLeitura;
    private bool _semIntervalos = true;
    private bool _mostrarOcultos;
    private bool _temOcultos;
    private int _quantidadeOcultos;
    private bool _refazerRegistro = true;
    private string _textoVazio = "Nenhum intervalo ainda. Parado, o programa não lê a janela.";
    private DateTime _diaInicial = DateTime.Today;
    private DateTime _diaFinal = DateTime.Today;
    private DateTime _referencia = DateTime.Today;
    private TipoDeFiltroDeDatas _filtro = TipoDeFiltroDeDatas.Dia;
    private bool _incluirFimDeSemana;
    private int _aplicandoPeriodo;
    private PaginaDaJanela _pagina = PaginaDaJanela.Registro;
    private ModoDoCalendario _modoCalendario = ModoDoCalendario.Escritorios;
    private bool _calendarioEmGrade;
    private bool _calendarioVertical;
    private string? _assinaturaCalendario;
    private string _legendaDoCalendario = "Cada faixa é o horário. O título é o escritório e o subtítulo é o que foi feito.";
    private string _textoDoCalendario = "";
    private string _totalDoRelatorio = "00:00:00";
    private string _totalPainelNaReuniao = "";
    private string _totalChat = "00:00:00";
    private string _totalChatNaReuniao = "";
    private string _totalDeReunioes = "00:00:00";
    private string _totalGeral = "00:00:00";
    private string _totalOutros = "00:00:00";
    private string _totalOutrosNaReuniao = "";

    public ModeloDaJanela(ServicoDeEsforco servico, ServicoDeVinculos vinculos, AtalhoDaJanela atalho)
    {
        _servico = servico;
        _vinculos = vinculos;
        _atalho = atalho;
        _atalho.Mudou += NotificarAtalho;
        AbrirVinculos = new Comando(AoAbrirVinculos, () => true);
        AbrirCategorias = new Comando(AoAbrirCategorias, () => true);
        AbrirAtalho = new Comando(AoAbrirAtalho, () => true);
        Iniciar = new Comando(AoIniciar, () => !_servico.EmLeitura);
        Parar = new Comando(AoParar, () => _servico.EmLeitura);
        RecolherTudo = new Comando(() => DefinirTodos(false), () => Nos.Count > 0);
        AbrirTudo = new Comando(() => DefinirTodos(true), () => Nos.Count > 0);
        AlternarOcultos = new Comando(AoAlternarOcultos, () => _temOcultos);
        ZerarPeriodo = new Comando(AoZerarPeriodo, () => true);
        DiaAnterior = new Comando(() => Deslocar(-1), () => true);
        DiaSeguinte = new Comando(() => Deslocar(1), () => true);
        IrParaHoje = new Comando(() => AplicarFiltro(TipoDeFiltroDeDatas.Dia, DateTime.Today), () => true);
        UsarFiltroSemanal = new Comando(() => AplicarFiltro(TipoDeFiltroDeDatas.Semanal, _referencia), () => true);
        UsarFiltroMensal = new Comando(() => AplicarFiltro(TipoDeFiltroDeDatas.Mensal, _referencia), () => true);
        UsarFiltroTrimestral = new Comando(() => AplicarFiltro(TipoDeFiltroDeDatas.Trimestral, _referencia), () => true);
        UsarFiltroSemestral = new Comando(() => AplicarFiltro(TipoDeFiltroDeDatas.Semestral, _referencia), () => true);
        UsarFiltroAnual = new Comando(() => AplicarFiltro(TipoDeFiltroDeDatas.Anual, _referencia), () => true);
        VerRegistro = new Comando(() => DefinirPagina(PaginaDaJanela.Registro), () => true);
        VerCalendario = new Comando(() => DefinirPagina(PaginaDaJanela.Calendario), () => true);
        VerEscritorios = new Comando(() => DefinirPagina(PaginaDaJanela.Escritorios), () => true);
        UsarVistaDiaria = new Comando(() => AplicarFiltro(TipoDeFiltroDeDatas.Dia, _referencia), () => true);
        UsarVistaSemanal = new Comando(() => AplicarFiltro(TipoDeFiltroDeDatas.Semanal, _referencia), () => true);
        UsarVistaMensal = new Comando(() => AplicarFiltro(TipoDeFiltroDeDatas.Mensal, _referencia), () => true);
        UsarModoEscritorios = new Comando(() => DefinirModo(ModoDoCalendario.Escritorios), () => true);
        UsarModoProgramas = new Comando(() => DefinirModo(ModoDoCalendario.Programas), () => true);
        UsarModoTelas = new Comando(() => DefinirModo(ModoDoCalendario.Telas), () => true);
        CopiarTransferencia = new Comando(AoCopiarTransferencia, () => Escritorios.Any(item => item.Id.Length > 0));
        AbrirOcultos = new Comando(AoAbrirOcultos, () => true);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<NoVm> Nos { get; } = new();

    public ICommand Iniciar { get; }

    public ICommand Parar { get; }

    public ICommand RecolherTudo { get; }

    public ICommand AbrirTudo { get; }

    public ICommand AlternarOcultos { get; }

    public ICommand ZerarPeriodo { get; }

    public ICommand DiaAnterior { get; }

    public ICommand DiaSeguinte { get; }

    public ICommand IrParaHoje { get; }

    public ICommand UsarFiltroSemanal { get; }

    public ICommand UsarFiltroMensal { get; }

    public ICommand UsarFiltroTrimestral { get; }

    public ICommand UsarFiltroSemestral { get; }

    public ICommand UsarFiltroAnual { get; }

    public string RotuloDoFiltro => _filtro switch
    {
        TipoDeFiltroDeDatas.Dia when _diaInicial.Date == DateTime.Today && _diaFinal.Date == DateTime.Today => "Hoje",
        TipoDeFiltroDeDatas.Dia => "Dia",
        TipoDeFiltroDeDatas.Semanal => "Semanal",
        TipoDeFiltroDeDatas.Mensal => "Mensal",
        TipoDeFiltroDeDatas.Trimestral => "Trimestral",
        TipoDeFiltroDeDatas.Semestral => "Semestral",
        TipoDeFiltroDeDatas.Anual => "Anual",
        _ => "Datas",
    };

    public bool EhFiltroHoje =>
        _filtro == TipoDeFiltroDeDatas.Dia
        && _diaInicial.Date == DateTime.Today
        && _diaFinal.Date == DateTime.Today;

    public bool EhFiltroSemanal => _filtro == TipoDeFiltroDeDatas.Semanal;

    public bool EhFiltroMensal => _filtro == TipoDeFiltroDeDatas.Mensal;

    public bool EhFiltroTrimestral => _filtro == TipoDeFiltroDeDatas.Trimestral;

    public bool EhFiltroSemestral => _filtro == TipoDeFiltroDeDatas.Semestral;

    public bool EhFiltroAnual => _filtro == TipoDeFiltroDeDatas.Anual;

    public bool EhVistaDiaria => _filtro == TipoDeFiltroDeDatas.Dia;

    public bool EhVistaSemanal => _filtro == TipoDeFiltroDeDatas.Semanal;

    public bool EhVistaMensal => _filtro == TipoDeFiltroDeDatas.Mensal;

    public bool EhModoEscritorios => _modoCalendario == ModoDoCalendario.Escritorios;

    public bool EhModoProgramas => _modoCalendario == ModoDoCalendario.Programas;

    public bool EhModoTelas => _modoCalendario == ModoDoCalendario.Telas;

    public bool IncluirFimDeSemana
    {
        get => _incluirFimDeSemana;
        set
        {
            if (_incluirFimDeSemana == value)
                return;

            _incluirFimDeSemana = value;
            Notificar(nameof(IncluirFimDeSemana));
            if (_filtro == TipoDeFiltroDeDatas.Semanal)
                AplicarFiltro(TipoDeFiltroDeDatas.Semanal, _referencia);
            else
            {
                _assinaturaCalendario = null;
                AtualizarCalendario();
            }
        }
    }

    public DateTime DiaInicial
    {
        get => NoMeioDoDia(_diaInicial);
        set => EditarPeriodo(value, _diaFinal, mudouInicio: true);
    }

    public DateTime DiaFinal
    {
        get => NoMeioDoDia(_diaFinal);
        set => EditarPeriodo(_diaInicial, value, mudouInicio: false);
    }

    public string Situacao => _emLeitura ? "Lendo a janela da frente" : "Parado";

    public bool EmLeitura => _emLeitura;

    public string TextoOcultos => _mostrarOcultos ? "Ocultos à vista" : $"Ocultos ({_quantidadeOcultos})";

    public string TextoVazio => _textoVazio;

    public bool TemOcultos => _temOcultos;

    public bool SemIntervalos => _semIntervalos;

    public bool TemIntervalos => !_semIntervalos;

    public bool NaPaginaDeRegistro => _pagina == PaginaDaJanela.Registro;

    public bool NaPaginaDeCalendario => _pagina == PaginaDaJanela.Calendario;

    public bool NaPaginaDeRelatorio => _pagina == PaginaDaJanela.Escritorios;

    public bool MostrarRegistro => NaPaginaDeRegistro && !_semIntervalos;

    public bool MostrarVazioDoRegistro => NaPaginaDeRegistro && _semIntervalos;

    public bool MostrarRelatorio => NaPaginaDeRelatorio && Escritorios.Count > 0;

    public bool MostrarVazioDoRelatorio => NaPaginaDeRelatorio && Escritorios.Count == 0;

    public bool MostrarDia => NaPaginaDeCalendario && _calendarioVertical;

    public bool MostrarLinhas => NaPaginaDeCalendario && !_calendarioEmGrade && !_calendarioVertical;

    public bool MostrarGrade => NaPaginaDeCalendario && _calendarioEmGrade;

    public string LegendaDoCalendario => _legendaDoCalendario;

    public string TextoDoCalendario => _textoDoCalendario;

    public bool TemAvisoDoCalendario => _textoDoCalendario.Length > 0;

    public string TotalDoRelatorio => _totalDoRelatorio;

    public string TotalPainelNaReuniao => _totalPainelNaReuniao;

    public bool TemPainelNaReuniao => _totalPainelNaReuniao.Length > 0;

    public string TotalChat => _totalChat;

    public string TotalChatNaReuniao => _totalChatNaReuniao;

    public bool TemChatNaReuniao => _totalChatNaReuniao.Length > 0;

    public string TotalDeReunioes => _totalDeReunioes;

    public string TotalGeral => _totalGeral;

    public ObservableCollection<EscritorioVm> Escritorios { get; } = new();

    public ICommand VerRegistro { get; }

    public ICommand VerCalendario { get; }

    public ICommand VerEscritorios { get; }

    public ICommand UsarVistaDiaria { get; }

    public ICommand UsarVistaSemanal { get; }

    public ICommand UsarVistaMensal { get; }

    public ICommand UsarModoEscritorios { get; }

    public ICommand UsarModoProgramas { get; }

    public ICommand UsarModoTelas { get; }

    public ObservableCollection<DiaCalendarioVm> DiasDoCalendario { get; } = new();

    public ObservableCollection<string> HorasDoCalendario { get; } = new();

    public ObservableCollection<string> CabecalhosDoCalendario { get; } = new();

    public ICommand CopiarTransferencia { get; }

    public ICommand AbrirOcultos { get; }

    public ICommand AbrirVinculos { get; }

    public ICommand AbrirCategorias { get; }

    public ICommand AbrirAtalho { get; }

    public string TextoDoAtalho => _atalho.Registrado
        ? $"{_atalho.Atual} abre a janelinha de vínculo para a janela que estiver na frente. Só funciona com o Esforço aberto."
        : $"O Windows recusou o atalho {_atalho.Atual}, porque outro programa já usa essa combinação. Troque em Vínculos, Atalho de vínculo.";

    public string RotuloDoAtalho => $"Atalho de vínculo ({_atalho.Atual})…";

    public bool AtalhoRecusado => !_atalho.Registrado;

    public string TotalOutros => _totalOutros;

    public string TotalOutrosNaReuniao => _totalOutrosNaReuniao;

    public bool TemOutrosNaReuniao => _totalOutrosNaReuniao.Length > 0;

    /// <summary>
    /// Chamado quando o Windows avisa que o atalho foi apertado. Guarda a janela da frente,
    /// para o tempo seguir contando para ela, e só então mostra a janelinha.
    /// </summary>
    public async void AbrirVinculoPeloAtalho()
    {
        if (_janelinha is not null)
        {
            _janelinha.Activate();
            return;
        }

        if (_abrindoVinculo)
            return;

        _abrindoVinculo = true;
        try
        {
            var alvo = await Task.Run(() => _servico.SegurarJanelaDaFrente(JanelaDeVinculo.TituloDaJanela));
            var janela = JanelaDeVinculo.DoAtalho(_servico, _vinculos, alvo);
            janela.Closed += (_, _) =>
            {
                _janelinha = null;
                _servico.SoltarJanelaDaFrente();
                if (janela.Salvou)
                    RecarregarTudo();
            };
            _janelinha = janela;
            janela.Show();
            janela.Activate();
        }
        catch (Exception)
        {
            _janelinha = null;
            _servico.SoltarJanelaDaFrente();
        }
        finally
        {
            _abrindoVinculo = false;
        }
    }

    public void AtualizarDuranteLeitura()
    {
        if (!_servico.EmLeitura)
            return;

        Atualizar();
    }

    /// <summary>
    /// Só a página à vista é refeita. As outras são trazidas em dia quando a pessoa troca para elas,
    /// em <see cref="DefinirPagina"/>. Com a leitura ligada isto roda a cada segundo.
    /// </summary>
    public void Atualizar()
    {
        var emLeitura = _servico.EmLeitura;
        if (emLeitura != _emLeitura)
        {
            _emLeitura = emLeitura;
            Notificar(nameof(Situacao));
            Notificar(nameof(EmLeitura));
            CommandManager.InvalidateRequerySuggested();
        }

        switch (_pagina)
        {
            case PaginaDaJanela.Registro:
                AtualizarRegistro();
                break;
            case PaginaDaJanela.Calendario:
                AtualizarCalendario();
                break;
            default:
                AtualizarRelatorio();
                break;
        }
    }

    private void AtualizarRegistro()
    {
        var resumo = _servico.ConsultarResumo(_diaInicial, _diaFinal);
        var agora = DateTimeOffset.Now;
        if (_refazerRegistro)
        {
            _refazerRegistro = false;
            Nos.Clear();
        }

        var ocultos = ContarOcultos(resumo);
        SincronizarNos(Visiveis(resumo), Nos, "", agora, 0, resumo);
        DefinirOcultos(ocultos);
        DefinirVazio(Nos.Count == 0, ocultos > 0 && !_mostrarOcultos, resumo.TemRegistros);
    }

    private void DefinirPagina(PaginaDaJanela pagina)
    {
        if (_pagina == pagina)
            return;

        _pagina = pagina;
        Atualizar();
        Notificar(nameof(NaPaginaDeRegistro));
        Notificar(nameof(NaPaginaDeCalendario));
        Notificar(nameof(NaPaginaDeRelatorio));
        NotificarPaginas();
    }

    private void DefinirModo(ModoDoCalendario modo)
    {
        if (_modoCalendario == modo)
            return;

        _modoCalendario = modo;
        Notificar(nameof(EhModoEscritorios));
        Notificar(nameof(EhModoProgramas));
        Notificar(nameof(EhModoTelas));
        _assinaturaCalendario = null;
        AtualizarCalendario();
    }

    private void AtualizarCalendario()
    {
        var quadro = _servico.ConsultarCalendario(
            _diaInicial,
            _diaFinal,
            _modoCalendario,
            emGrade: _diaFinal.Date >= _diaInicial.Date.AddDays(7),
            deDomingoASabado: _incluirFimDeSemana);
        DefinirForma(quadro.EmGrade, quadro.Vertical);
        DefinirLegenda();
        DefinirAviso(quadro);

        var assinatura = AssinarCalendario(quadro);
        if (assinatura == _assinaturaCalendario)
            return;

        if (AplicarCalendario(quadro))
        {
            _assinaturaCalendario = assinatura;
            return;
        }

        _assinaturaCalendario = assinatura;
        HorasDoCalendario.Clear();
        for (var hora = quadro.HoraInicial; hora < quadro.HoraFinal; hora++)
            HorasDoCalendario.Add($"{hora:00}");

        CabecalhosDoCalendario.Clear();
        foreach (var cabecalho in quadro.Cabecalhos)
            CabecalhosDoCalendario.Add(cabecalho);

        DiasDoCalendario.Clear();
        foreach (var dia in quadro.Dias)
            DiasDoCalendario.Add(new DiaCalendarioVm(dia, quadro.HoraInicial, quadro.HoraFinal, AbrirDia, AbrirVinculoDoBloco));
    }

    private void AbrirDia(DateTime dia) => AplicarFiltro(TipoDeFiltroDeDatas.Dia, dia.Date);

    /// <summary>
    /// Clique numa faixa do calendário. Numa faixa fundida o alvo cobre o período inteiro dela.
    /// </summary>
    private async void AbrirVinculoDoBloco(AlvoDeVinculo alvo)
    {
        if (_janelinha is not null)
        {
            _janelinha.Activate();
            return;
        }

        if (_abrindoVinculo)
            return;

        _abrindoVinculo = true;
        try
        {
            var alcance = _modoCalendario == ModoDoCalendario.Programas ? AlcanceDoVinculo.Programa : (AlcanceDoVinculo?)null;
            var janela = JanelaDeVinculo.DoCalendario(_servico, _vinculos, alvo, alcance);
            if (await janela.MostrarSegurando(Application.Current?.MainWindow))
                RecarregarTudo();
        }
        finally
        {
            _abrindoVinculo = false;
        }
    }

    private void AoAbrirVinculos()
    {
        if (_janelinha is not null || _abrindoVinculo)
            return;

        var dialogo = new JanelaDeVinculos(_servico, _vinculos)
        {
            Owner = Application.Current?.MainWindow,
            WindowStartupLocation = Application.Current?.MainWindow is null
                ? WindowStartupLocation.CenterScreen
                : WindowStartupLocation.CenterOwner,
        };
        _abrindoVinculo = true;
        try
        {
            dialogo.ShowDialog();
        }
        finally
        {
            _abrindoVinculo = false;
        }

        RecarregarTudo();
    }

    private void AoAbrirCategorias()
    {
        var dialogo = new JanelaDeCategorias(_vinculos)
        {
            Owner = Application.Current?.MainWindow,
            WindowStartupLocation = Application.Current?.MainWindow is null
                ? WindowStartupLocation.CenterScreen
                : WindowStartupLocation.CenterOwner,
        };
        dialogo.ShowDialog();
        RecarregarTudo();
    }

    private void AoAbrirAtalho()
    {
        var dialogo = new JanelaDeAtalho(_atalho)
        {
            Owner = Application.Current?.MainWindow,
            WindowStartupLocation = Application.Current?.MainWindow is null
                ? WindowStartupLocation.CenterScreen
                : WindowStartupLocation.CenterOwner,
        };
        dialogo.ShowDialog();
    }

    private void NotificarAtalho()
    {
        Notificar(nameof(TextoDoAtalho));
        Notificar(nameof(RotuloDoAtalho));
        Notificar(nameof(AtalhoRecusado));
    }

    private void RecarregarTudo()
    {
        _refazerRegistro = true;
        _assinaturaCalendario = null;
        Atualizar();
        CommandManager.InvalidateRequerySuggested();
    }

    private bool AplicarCalendario(QuadroDeCalendario quadro)
    {
        if (DiasDoCalendario.Count != quadro.Dias.Count || DiasDoCalendario.Count == 0)
            return false;
        if (HorasDoCalendario.Count != quadro.HoraFinal - quadro.HoraInicial)
            return false;
        if (HorasDoCalendario.Count > 0 && HorasDoCalendario[0] != $"{quadro.HoraInicial:00}")
            return false;
        if (CabecalhosDoCalendario.Count != quadro.Cabecalhos.Count)
            return false;

        for (var i = 0; i < quadro.Dias.Count; i++)
        {
            var dia = quadro.Dias[i];
            var vm = DiasDoCalendario[i];
            if (vm.Numero != dia.Numero.ToString() || vm.Blocos.Count != dia.Blocos.Count)
                return false;

            for (var bloco = 0; bloco < dia.Blocos.Count; bloco++)
            {
                var novo = dia.Blocos[bloco];
                var atual = vm.Blocos[bloco];
                if (atual.InicioMinuto != novo.InicioMinuto
                    || atual.Trilha != novo.Trilha
                    || atual.Titulo != novo.Titulo
                    || atual.Subtitulo != novo.Subtitulo
                    || atual.Tipo != novo.Tipo)
                    return false;
            }
        }

        for (var i = 0; i < quadro.Dias.Count; i++)
        {
            var dia = quadro.Dias[i];
            var vm = DiasDoCalendario[i];
            vm.DefinirDuracao(dia.Duracao);
            for (var bloco = 0; bloco < dia.Blocos.Count; bloco++)
                vm.Blocos[bloco].Atualizar(dia.Blocos[bloco]);
        }

        return true;
    }

    private void DefinirForma(bool grade, bool vertical)
    {
        if (_calendarioEmGrade == grade && _calendarioVertical == vertical)
            return;

        _calendarioEmGrade = grade;
        _calendarioVertical = vertical;
        Notificar(nameof(MostrarDia));
        Notificar(nameof(MostrarLinhas));
        Notificar(nameof(MostrarGrade));
    }

    private void DefinirLegenda()
    {
        var texto = _modoCalendario switch
        {
            ModoDoCalendario.Programas => "Cada coluna é um dia e as horas descem nela. O título é o programa e o subtítulo é a tela. Clique numa faixa para vincular a um escritório.",
            ModoDoCalendario.Telas => "Cada coluna é um dia e as horas descem nela. O título é a tela e o subtítulo é o programa. Clique numa faixa para vincular a um escritório.",
            _ => "Cada coluna é um dia e as horas descem nela. O símbolo marca painel, Chatwoot, reunião ou vínculo manual. O título é o escritório e o subtítulo é o que foi feito. Clique numa faixa para vincular a um escritório.",
        };
        if (_legendaDoCalendario == texto)
            return;

        _legendaDoCalendario = texto;
        Notificar(nameof(LegendaDoCalendario));
    }

    private void DefinirAviso(QuadroDeCalendario quadro)
    {
        var vazio = quadro.Dias.All(dia => dia.Blocos.Count == 0);
        var texto = !vazio
            ? ""
            : _modoCalendario switch
            {
                ModoDoCalendario.Programas => "Nenhum programa neste período.",
                ModoDoCalendario.Telas => "Nenhuma tela neste período.",
                _ => "Nada de escritório neste período.",
            };
        if (_textoDoCalendario == texto)
            return;

        _textoDoCalendario = texto;
        Notificar(nameof(TextoDoCalendario));
        Notificar(nameof(TemAvisoDoCalendario));
    }

    private static string AssinarCalendario(QuadroDeCalendario quadro)
    {
        var texto = new StringBuilder();
        texto.Append(quadro.EmGrade).Append(quadro.Vertical).Append(quadro.HoraInicial).Append(quadro.HoraFinal);
        foreach (var cabecalho in quadro.Cabecalhos)
            texto.Append(cabecalho);
        foreach (var dia in quadro.Dias)
        {
            texto.Append(dia.Dia.ToString("yyyyMMdd")).Append(dia.NoPeriodo).Append(dia.Duracao);
            foreach (var bloco in dia.Blocos)
            {
                texto.Append('|').Append(bloco.InicioMinuto).Append('-').Append(bloco.FimMinuto);
                texto.Append('#').Append(bloco.Trilha).Append(bloco.Titulo).Append(bloco.Subtitulo).Append(bloco.Tipo);
            }
        }

        return texto.ToString();
    }

    private void AtualizarRelatorio()
    {
        var linhas = _servico.ConsultarEscritorios(_diaInicial, _diaFinal);
        var totalPainel = TimeSpan.Zero;
        var totalNaReuniao = TimeSpan.Zero;
        var totalChat = TimeSpan.Zero;
        var totalChatNaReuniao = TimeSpan.Zero;
        var totalReunioes = TimeSpan.Zero;
        var totalOutros = TimeSpan.Zero;
        var totalOutrosNaReuniao = TimeSpan.Zero;
        foreach (var linha in linhas)
        {
            totalPainel += linha.DuracaoPainel;
            totalNaReuniao += linha.DuracaoPainelNaReuniao;
            totalChat += linha.DuracaoChat;
            totalChatNaReuniao += linha.DuracaoChatNaReuniao;
            totalReunioes += linha.DuracaoReunioes;
            totalOutros += linha.DuracaoOutros;
            totalOutrosNaReuniao += linha.DuracaoOutrosNaReuniao;
        }

        var totalGeral = totalPainel + totalChat + totalReunioes + totalOutros;

        if (MesmasLinhas(linhas))
        {
            var editando = Escritorios.Any(item => item.Aberto);
            foreach (var linha in linhas)
            {
                var vm = Escritorios.First(item => item.Chave == linha.Chave);
                vm.Aplicar(linha, Parte(Contado(linha), totalGeral));
            }

            if (!editando)
                Reordenar(linhas);
        }
        else
        {
            var estado = Escritorios.ToDictionary(item => item.Chave, item => item.Capturar());
            Escritorios.Clear();
            foreach (var linha in linhas)
            {
                var vm = new EscritorioVm(_servico, Atualizar);
                vm.Aplicar(linha, Parte(Contado(linha), totalGeral));
                if (estado.TryGetValue(vm.Chave, out var capturado))
                    vm.Restaurar(capturado);
                Escritorios.Add(vm);
            }

            NotificarPaginas();
        }

        DefinirTotal(ref _totalDoRelatorio, Formatar.Duracao(totalPainel), nameof(TotalDoRelatorio));
        DefinirTotal(
            ref _totalPainelNaReuniao,
            totalNaReuniao > TimeSpan.Zero ? Formatar.Duracao(totalNaReuniao) : "",
            nameof(TotalPainelNaReuniao));
        Notificar(nameof(TemPainelNaReuniao));
        DefinirTotal(ref _totalChat, Formatar.Duracao(totalChat), nameof(TotalChat));
        DefinirTotal(
            ref _totalChatNaReuniao,
            totalChatNaReuniao > TimeSpan.Zero ? Formatar.Duracao(totalChatNaReuniao) : "",
            nameof(TotalChatNaReuniao));
        Notificar(nameof(TemChatNaReuniao));
        DefinirTotal(ref _totalDeReunioes, Formatar.Duracao(totalReunioes), nameof(TotalDeReunioes));
        DefinirTotal(ref _totalOutros, Formatar.Duracao(totalOutros), nameof(TotalOutros));
        DefinirTotal(
            ref _totalOutrosNaReuniao,
            totalOutrosNaReuniao > TimeSpan.Zero ? Formatar.Duracao(totalOutrosNaReuniao) : "",
            nameof(TotalOutrosNaReuniao));
        Notificar(nameof(TemOutrosNaReuniao));
        DefinirTotal(ref _totalGeral, Formatar.Duracao(totalGeral), nameof(TotalGeral));
        CommandManager.InvalidateRequerySuggested();
    }

    private void DefinirTotal(ref string campo, string texto, string nome)
    {
        if (campo == texto)
            return;

        campo = texto;
        Notificar(nome);
    }

    private bool MesmasLinhas(IReadOnlyList<LinhaDeEscritorio> linhas)
    {
        if (linhas.Count != Escritorios.Count)
            return false;

        var chaves = Escritorios.Select(item => item.Chave).ToHashSet(StringComparer.Ordinal);
        return linhas.All(linha => chaves.Contains(linha.Chave));
    }

    private void Reordenar(IReadOnlyList<LinhaDeEscritorio> linhas)
    {
        for (var alvo = 0; alvo < linhas.Count; alvo++)
        {
            var atual = -1;
            for (var i = 0; i < Escritorios.Count; i++)
            {
                if (Escritorios[i].Chave != linhas[alvo].Chave)
                    continue;

                atual = i;
                break;
            }

            if (atual >= 0 && atual != alvo)
                Escritorios.Move(atual, alvo);
        }
    }

    private void AoAbrirOcultos()
    {
        var dialogo = new JanelaDeOcultos(_servico)
        {
            Owner = Application.Current?.MainWindow,
            WindowStartupLocation = Application.Current?.MainWindow is null
                ? WindowStartupLocation.CenterScreen
                : WindowStartupLocation.CenterOwner,
        };
        dialogo.ShowDialog();
        Atualizar();
    }

    private void AoCopiarTransferencia()
    {
        var texto = TabelaDeTransferencia.Montar(
            Escritorios.Select(item => (item.Id, item.Parte, item.Rotulo)));
        if (texto.Length == 0)
            return;

        Clipboard.SetText(texto);
    }

    private static TimeSpan Contado(LinhaDeEscritorio linha) => linha.DuracaoContada;

    private static string Parte(TimeSpan duracao, TimeSpan total)
    {
        if (total <= TimeSpan.Zero)
            return "0%";

        var percentual = 100.0 * duracao.Ticks / total.Ticks;
        return $"{Math.Round(percentual):0}%";
    }

    private void NotificarPaginas()
    {
        Notificar(nameof(MostrarRegistro));
        Notificar(nameof(MostrarVazioDoRegistro));
        Notificar(nameof(MostrarRelatorio));
        Notificar(nameof(MostrarVazioDoRelatorio));
        Notificar(nameof(MostrarDia));
        Notificar(nameof(MostrarLinhas));
        Notificar(nameof(MostrarGrade));
    }

    private void AoIniciar()
    {
        _servico.Iniciar();
        Atualizar();
        CommandManager.InvalidateRequerySuggested();
    }

    private void AoParar()
    {
        _servico.Parar();
        Atualizar();
        CommandManager.InvalidateRequerySuggested();
    }

    private void Deslocar(int direcao)
    {
        var (inicio, fim) = FiltroDeDatas.Deslocar(_filtro, _diaInicial, _diaFinal, direcao, _incluirFimDeSemana);
        _referencia = FiltroDeDatas.ReferenciaAoDeslocar(_referencia, _diaInicial, inicio, fim);
        DefinirPeriodo(inicio, fim);
    }

    private void AplicarFiltro(TipoDeFiltroDeDatas tipo, DateTime referencia)
    {
        _referencia = referencia.Date;
        _filtro = tipo;
        var (inicio, fim) = FiltroDeDatas.Do(tipo, _referencia, _incluirFimDeSemana);
        DefinirPeriodo(inicio, fim);
    }

    private void EditarPeriodo(DateTime inicio, DateTime fim, bool mudouInicio)
    {
        if (_aplicandoPeriodo > 0)
            return;

        var editado = (mudouInicio ? inicio : fim).Date;
        inicio = inicio.Date;
        fim = fim.Date;
        if (fim < inicio)
            (inicio, fim) = (fim, inicio);

        if (inicio == _diaInicial.Date && fim == _diaFinal.Date)
            return;

        if (_referencia < inicio || _referencia > fim)
            _referencia = editado >= inicio && editado <= fim ? editado : inicio;

        DefinirPeriodo(inicio, fim, manual: true);
    }

    private void DefinirPeriodo(DateTime inicio, DateTime fim, bool manual = false)
    {
        inicio = inicio.Date;
        fim = fim.Date;
        if (fim < inicio)
            (inicio, fim) = (fim, inicio);

        if (inicio == _diaInicial && fim == _diaFinal)
        {
            NotificarFiltro();
            return;
        }

        if (manual)
            _filtro = TipoDeFiltroDeDatas.Personalizado;

        _diaInicial = inicio;
        _diaFinal = fim;
        _aplicandoPeriodo++;
        Notificar(nameof(DiaInicial));
        Notificar(nameof(DiaFinal));
        NotificarFiltro();
        _refazerRegistro = true;
        Atualizar();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(SoltarAjuste));
    }

    private void SoltarAjuste()
    {
        if (_aplicandoPeriodo > 0)
            _aplicandoPeriodo--;
    }

    private static DateTime NoMeioDoDia(DateTime dia) =>
        DateTime.SpecifyKind(dia.Date.AddHours(12), DateTimeKind.Local);

    private void NotificarFiltro()
    {
        Notificar(nameof(RotuloDoFiltro));
        Notificar(nameof(EhFiltroHoje));
        Notificar(nameof(EhFiltroSemanal));
        Notificar(nameof(EhFiltroMensal));
        Notificar(nameof(EhFiltroTrimestral));
        Notificar(nameof(EhFiltroSemestral));
        Notificar(nameof(EhFiltroAnual));
        Notificar(nameof(EhVistaDiaria));
        Notificar(nameof(EhVistaSemanal));
        Notificar(nameof(EhVistaMensal));
    }

    private void AoZerarPeriodo()
    {
        var dialogo = new JanelaDePeriodo
        {
            Owner = Application.Current?.MainWindow,
            WindowStartupLocation = Application.Current?.MainWindow is null
                ? WindowStartupLocation.CenterScreen
                : WindowStartupLocation.CenterOwner,
        };
        if (dialogo.ShowDialog() != true)
            return;

        _servico.ZerarPeriodo(dialogo.Inicio, dialogo.Fim);
        Recarregar();
    }

    private void AoAlternarOcultos()
    {
        _mostrarOcultos = !_mostrarOcultos;
        _refazerRegistro = true;
        Atualizar();
        Notificar(nameof(TextoOcultos));
    }

    private static PreferenciaDePrograma PreferenciaDe(ResumoDeEsforco resumo, string programa) =>
        resumo.Preferencias.TryGetValue(programa, out var preferencia)
            ? preferencia
            : PreferenciaDePrograma.Vazia(programa);

    private int ContarOcultos(ResumoDeEsforco resumo)
    {
        var total = 0;
        foreach (var dia in resumo.Nos)
        {
            foreach (var programa in dia.Filhos)
            {
                if (PreferenciaDe(resumo, programa.Rotulo).Oculto)
                    total++;
            }
        }

        return total;
    }

    private IReadOnlyList<NoDeEsforco> Visiveis(ResumoDeEsforco resumo)
    {
        var dias = new List<NoDeEsforco>();
        foreach (var dia in resumo.Nos)
        {
            var programas = dia.Filhos
                .Where(programa => _mostrarOcultos || !PreferenciaDe(resumo, programa.Rotulo).Oculto)
                .ToList();
            if (programas.Count == 0)
                continue;

            var duracao = TimeSpan.Zero;
            foreach (var programa in programas)
                duracao += programa.DuracaoTotal;
            dias.Add(dia with { Filhos = programas, DuracaoTotal = duracao });
        }

        return dias;
    }

    private void PausarPrograma(string programa)
    {
        _servico.AlternarPausado(programa);
        Recarregar();
    }

    private void FixarPrograma(string programa)
    {
        _servico.AlternarFixado(programa);
        Recarregar();
    }

    private void OcultarPrograma(string programa)
    {
        _servico.AlternarOculto(programa);
        Recarregar();
    }

    private void ExcluirPrograma(string programa)
    {
        var resposta = MessageBox.Show(
            $"Excluir o tempo já gravado de {programa}? Se ele continuar na frente e a coleta não estiver pausada, um intervalo novo começa.",
            "Esforço",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning);
        if (resposta != MessageBoxResult.OK)
            return;

        _servico.ExcluirPrograma(programa);
        Recarregar();
    }

    private void Recarregar()
    {
        _refazerRegistro = true;
        Atualizar();
        CommandManager.InvalidateRequerySuggested();
    }

    private NoVm Criar(NoDeEsforco no, string prefixo, DateTimeOffset agora, int nivel, ResumoDeEsforco resumo)
    {
        var chave = prefixo + "/" + no.Rotulo;
        var ehPrograma = nivel == 1;
        var vm = new NoVm(
            no.Rotulo,
            Formatar.Duracao(no.DuracaoTotal),
            chave,
            AbertoInicial(chave, nivel == 0),
            GuardarExpansao,
            ehPrograma,
            ehPrograma ? PreferenciaDe(resumo, no.Rotulo) : PreferenciaDePrograma.Vazia(no.Rotulo),
            PausarPrograma,
            FixarPrograma,
            OcultarPrograma,
            ExcluirPrograma);

        foreach (var intervalo in no.Intervalos)
            vm.Intervalos.Add(CriarLinha(intervalo, agora));

        foreach (var filho in no.Filhos)
            vm.Filhos.Add(Criar(filho, chave, agora, nivel + 1, resumo));

        vm.AtualizarConteudo();
        return vm;
    }

    private static LinhaIntervaloVm CriarLinha(IntervaloDeEsforco intervalo, DateTimeOffset agora) =>
        new(
            intervalo.Id ?? 0,
            intervalo.Inicio,
            intervalo.Fim,
            Formatar.Instante(intervalo.Inicio),
            Formatar.Fim(intervalo.Fim),
            Formatar.Duracao(intervalo.DuracaoAte(agora)),
            EscritorioDe(intervalo),
            JanelaDe(intervalo),
            intervalo.DuranteReuniao,
            intervalo.EhChatwoot,
            intervalo.EscritorioVinculado.Length > 0);

    private static string EscritorioDe(IntervaloDeEsforco intervalo) =>
        intervalo.EscritorioVinculado.Length > 0
            ? $"{intervalo.EscritorioVinculado} (vínculo)"
            : string.IsNullOrWhiteSpace(intervalo.Escritorio) ? "vazio" : intervalo.Escritorio;

    private static string JanelaDe(IntervaloDeEsforco intervalo) => intervalo.Minimizada ? "minimizada" : "ativa";

    /// <summary>
    /// Traz a árvore da tela para o que o resumo diz, mexendo só no que mudou. Refazer tudo custa
    /// centenas de elementos de tela, e isto roda a cada segundo com a leitura ligada.
    /// </summary>
    private void SincronizarNos(
        IReadOnlyList<NoDeEsforco> nos,
        ObservableCollection<NoVm> vms,
        string prefixo,
        DateTimeOffset agora,
        int nivel,
        ResumoDeEsforco resumo)
    {
        if (!MesmosNos(nos, vms))
        {
            var rotulos = new HashSet<string>(StringComparer.Ordinal);
            foreach (var no in nos)
                rotulos.Add(no.Rotulo);

            for (var i = vms.Count - 1; i >= 0; i--)
            {
                if (!rotulos.Contains(vms[i].Rotulo))
                    vms.RemoveAt(i);
            }

            for (var i = 0; i < nos.Count; i++)
            {
                var atual = -1;
                for (var j = i; j < vms.Count; j++)
                {
                    if (vms[j].Rotulo != nos[i].Rotulo)
                        continue;

                    atual = j;
                    break;
                }

                if (atual < 0)
                    vms.Insert(i, Criar(nos[i], prefixo, agora, nivel, resumo));
                else if (atual != i)
                    vms.Move(atual, i);
            }

            while (vms.Count > nos.Count)
                vms.RemoveAt(vms.Count - 1);
        }

        for (var i = 0; i < nos.Count; i++)
        {
            var no = nos[i];
            var vm = vms[i];
            if (nivel == 1 && !vm.TemPreferencia(PreferenciaDe(resumo, no.Rotulo)))
            {
                vms[i] = Criar(no, prefixo, agora, nivel, resumo);
                continue;
            }

            vm.DuracaoTotal = Formatar.Duracao(no.DuracaoTotal);
            var tinhaConteudo = vm.TemConteudo;
            var tinhaIntervalos = vm.TemIntervalos;
            SincronizarLinhas(no.Intervalos, vm.Intervalos, agora);
            SincronizarNos(no.Filhos, vm.Filhos, vm.Chave, agora, nivel + 1, resumo);
            if (tinhaConteudo != vm.TemConteudo || tinhaIntervalos != vm.TemIntervalos)
                vm.AtualizarConteudo();
        }
    }

    private static bool MesmosNos(IReadOnlyList<NoDeEsforco> nos, ObservableCollection<NoVm> vms)
    {
        if (nos.Count != vms.Count)
            return false;

        for (var i = 0; i < nos.Count; i++)
        {
            if (vms[i].Rotulo != nos[i].Rotulo)
                return false;
        }

        return true;
    }

    private static void SincronizarLinhas(
        IReadOnlyList<IntervaloDeEsforco> intervalos,
        ObservableCollection<LinhaIntervaloVm> linhas,
        DateTimeOffset agora)
    {
        if (!MesmasLinhas(intervalos, linhas))
        {
            var chaves = new HashSet<(long, DateTimeOffset)>();
            foreach (var intervalo in intervalos)
                chaves.Add((intervalo.Id ?? 0, intervalo.Inicio));

            for (var i = linhas.Count - 1; i >= 0; i--)
            {
                if (!chaves.Contains((linhas[i].Id, linhas[i].Comeco)))
                    linhas.RemoveAt(i);
            }

            for (var i = 0; i < intervalos.Count; i++)
            {
                var atual = -1;
                for (var j = i; j < linhas.Count; j++)
                {
                    if (!linhas[j].EhDe(intervalos[i]))
                        continue;

                    atual = j;
                    break;
                }

                if (atual < 0)
                    linhas.Insert(i, CriarLinha(intervalos[i], agora));
                else if (atual != i)
                    linhas.Move(atual, i);
            }

            while (linhas.Count > intervalos.Count)
                linhas.RemoveAt(linhas.Count - 1);
        }

        for (var i = 0; i < intervalos.Count; i++)
        {
            var intervalo = intervalos[i];
            var linha = linhas[i];
            var vinculado = intervalo.EscritorioVinculado.Length > 0;
            if (linha.Vinculado != vinculado
                || linha.EhChat != intervalo.EhChatwoot
                || linha.Escritorio != EscritorioDe(intervalo)
                || linha.Janela != JanelaDe(intervalo))
            {
                linhas[i] = CriarLinha(intervalo, agora);
                continue;
            }

            linha.Atualizar(intervalo, agora);
        }
    }

    private static bool MesmasLinhas(IReadOnlyList<IntervaloDeEsforco> intervalos, ObservableCollection<LinhaIntervaloVm> linhas)
    {
        if (intervalos.Count != linhas.Count)
            return false;

        for (var i = 0; i < intervalos.Count; i++)
        {
            if (!linhas[i].EhDe(intervalos[i]))
                return false;
        }

        return true;
    }

    private bool AbertoInicial(string chave, bool raiz)
    {
        if (_expansao.TryGetValue(chave, out var guardado))
            return guardado;

        return _expansaoPadrao ?? raiz;
    }

    private void GuardarExpansao(string chave, bool aberto) => _expansao[chave] = aberto;

    private void DefinirTodos(bool aberto)
    {
        _expansaoPadrao = aberto;
        _expansao.Clear();
        DefinirExpansao(Nos, aberto);
    }

    private static void DefinirExpansao(IEnumerable<NoVm> nos, bool aberto)
    {
        foreach (var no in nos)
        {
            no.Expandido = aberto;
            DefinirExpansao(no.Filhos, aberto);
        }
    }

    private void DefinirVazio(bool vazio, bool tudoOculto, bool temRegistros)
    {
        var texto = tudoOculto
            ? "Os programas desta lista estão ocultos."
            : temRegistros
                ? "Nenhum intervalo neste período."
                : "Nenhum intervalo ainda. Parado, o programa não lê a janela.";
        if (_textoVazio != texto)
        {
            _textoVazio = texto;
            Notificar(nameof(TextoVazio));
        }

        if (_semIntervalos == vazio)
            return;

        _semIntervalos = vazio;
        Notificar(nameof(SemIntervalos));
        Notificar(nameof(TemIntervalos));
        NotificarPaginas();
    }

    private void DefinirOcultos(int quantidade)
    {
        var tem = quantidade > 0;
        if (_quantidadeOcultos != quantidade)
        {
            _quantidadeOcultos = quantidade;
            Notificar(nameof(TextoOcultos));
        }

        if (_temOcultos == tem)
            return;

        _temOcultos = tem;
        Notificar(nameof(TemOcultos));
        CommandManager.InvalidateRequerySuggested();
    }

    private void Notificar(string nome) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nome));

    private enum PaginaDaJanela
    {
        Registro,
        Calendario,
        Escritorios,
    }
}

public abstract class Observavel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void Definir<T>(ref T campo, T valor, [CallerMemberName] string? nome = null)
    {
        if (EqualityComparer<T>.Default.Equals(campo, valor))
            return;

        campo = valor;
        Notificar(nome);
    }

    protected void Notificar(string? nome) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nome));
}

public sealed class NoVm : Observavel
{
    private readonly Action<string, bool> _aoMudarExpansao;
    private readonly Action<string> _pausar;
    private readonly Action<string> _fixar;
    private readonly Action<string> _ocultar;
    private readonly Action<string> _excluir;
    private string _duracaoTotal;
    private bool _expandido;

    public NoVm(
        string rotulo,
        string duracaoTotal,
        string chave,
        bool expandido,
        Action<string, bool> aoMudarExpansao,
        bool ehRaiz,
        PreferenciaDePrograma preferencia,
        Action<string> pausar,
        Action<string> fixar,
        Action<string> ocultar,
        Action<string> excluir)
    {
        Rotulo = rotulo;
        _duracaoTotal = duracaoTotal;
        Chave = chave;
        _expandido = expandido;
        _aoMudarExpansao = aoMudarExpansao;
        EhPrograma = ehRaiz;
        Oculto = preferencia.Oculto;
        Fixado = preferencia.Fixado;
        Pausado = preferencia.Pausado;
        _pausar = pausar;
        _fixar = fixar;
        _ocultar = ocultar;
        _excluir = excluir;
        Alternar = new Comando(AlternarExpansao, () => true);
        Pausar = new Comando(() => _pausar(Rotulo), () => EhPrograma);
        Fixar = new Comando(() => _fixar(Rotulo), () => EhPrograma);
        Ocultar = new Comando(() => _ocultar(Rotulo), () => EhPrograma);
        Excluir = new Comando(() => _excluir(Rotulo), () => EhPrograma);
    }

    public string Rotulo { get; }

    public string Chave { get; }

    public ICommand Alternar { get; }

    public ICommand Pausar { get; }

    public ICommand Fixar { get; }

    public ICommand Ocultar { get; }

    public ICommand Excluir { get; }

    public bool EhPrograma { get; }

    public bool Oculto { get; }

    public bool Fixado { get; }

    public bool Pausado { get; }

    public string TextoPausa => Pausado ? "Retomar coleta" : "Pausar coleta";

    public string TextoFixar => Fixado ? "Desafixar" : "Fixar";

    public string TextoOcultar => Oculto ? "Mostrar" : "Ocultar";

    public string Marcas
    {
        get
        {
            var marcas = new List<string>();
            if (Fixado)
                marcas.Add("fixado");
            if (Pausado)
                marcas.Add("pausa");
            if (Oculto)
                marcas.Add("oculto");
            return string.Join(" · ", marcas);
        }
    }

    public bool TemMarcas => Marcas.Length > 0;

    public bool TemPreferencia(PreferenciaDePrograma preferencia) =>
        Oculto == preferencia.Oculto && Fixado == preferencia.Fixado && Pausado == preferencia.Pausado;

    public ObservableCollection<NoVm> Filhos { get; } = new();

    public ObservableCollection<LinhaIntervaloVm> Intervalos { get; } = new();

    public bool TemConteudo => Filhos.Count > 0 || Intervalos.Count > 0;

    public bool TemIntervalos => Intervalos.Count > 0;

    public bool Expandido
    {
        get => _expandido;
        set
        {
            if (_expandido == value)
                return;

            _expandido = value;
            Notificar(nameof(Expandido));
            Notificar(nameof(Seta));
            Notificar(nameof(ConteudoVisivel));
            Notificar(nameof(Dica));
            _aoMudarExpansao(Chave, value);
        }
    }

    public bool ConteudoVisivel => Expandido && TemConteudo;

    public string Seta => !TemConteudo ? "" : Expandido ? "\uE70D" : "\uE76C";

    public string Dica => !TemConteudo ? Rotulo : Expandido ? "Recolher" : "Abrir";

    public string DuracaoTotal
    {
        get => _duracaoTotal;
        set => Definir(ref _duracaoTotal, value);
    }

    public void AtualizarConteudo()
    {
        Notificar(nameof(TemConteudo));
        Notificar(nameof(TemIntervalos));
        Notificar(nameof(Seta));
        Notificar(nameof(ConteudoVisivel));
        Notificar(nameof(Dica));
    }

    private void AlternarExpansao()
    {
        if (TemConteudo)
            Expandido = !Expandido;
    }
}

public sealed class LinhaIntervaloVm : Observavel
{
    private string _fim;
    private string _duracao;
    private bool _duranteReuniao;
    private DateTimeOffset? _termino;

    public LinhaIntervaloVm(
        long id,
        DateTimeOffset comeco,
        DateTimeOffset? termino,
        string inicio,
        string fim,
        string duracao,
        string escritorio,
        string janela,
        bool duranteReuniao,
        bool ehChat = false,
        bool vinculado = false)
    {
        Vinculado = vinculado;
        Id = id;
        Comeco = comeco;
        _termino = termino;
        Inicio = inicio;
        _fim = fim;
        _duracao = duracao;
        Escritorio = escritorio;
        Janela = janela;
        _duranteReuniao = duranteReuniao;
        EhChat = ehChat;
    }

    /// <summary>
    /// Zero quando a linha é uma reunião juntada de vários registros, que não tem um id só.
    /// </summary>
    public long Id { get; }

    public DateTimeOffset Comeco { get; }

    public bool EhChat { get; }

    public bool Vinculado { get; }

    public bool EhDe(IntervaloDeEsforco intervalo) =>
        Id == (intervalo.Id ?? 0) && Comeco == intervalo.Inicio;

    public string Inicio { get; }

    public string Escritorio { get; }

    public string Janela { get; }

    public string Fim
    {
        get => _fim;
        private set => Definir(ref _fim, value);
    }

    public string Duracao
    {
        get => _duracao;
        private set => Definir(ref _duracao, value);
    }

    public bool DuranteReuniao
    {
        get => _duranteReuniao;
        private set
        {
            if (_duranteReuniao == value)
                return;

            _duranteReuniao = value;
            Notificar(nameof(DuranteReuniao));
            Notificar(nameof(Dica));
        }
    }

    public string? Dica => !DuranteReuniao
        ? Vinculado
            ? "Este escritório veio de um vínculo manual. O tempo entra em Outros trabalhos no relatório de Escritórios."
            : null
        : Vinculado
            ? "Este trabalho vinculado foi durante a reunião do mesmo escritório. O tempo continua nesta lista. No relatório de Escritórios, o trecho simultâneo não entra no total."
            : EhChat
            ? "Este Chatwoot foi durante a reunião do escritório. O tempo continua nesta lista. No relatório de Escritórios, o trecho simultâneo não entra no total."
            : "Este uso do painel foi durante a reunião do escritório. O tempo continua nesta lista. No relatório de Escritórios, o trecho simultâneo não entra no total.";

    /// <summary>
    /// Um intervalo fechado não muda mais de fim nem de duração: os textos só são refeitos
    /// para o que está aberto ou acabou de fechar.
    /// </summary>
    public void Atualizar(IntervaloDeEsforco intervalo, DateTimeOffset agora)
    {
        DuranteReuniao = intervalo.DuranteReuniao;
        if (intervalo.Fim is not null && intervalo.Fim == _termino)
            return;

        _termino = intervalo.Fim;
        Fim = Formatar.Fim(intervalo.Fim);
        Duracao = Formatar.Duracao(intervalo.DuracaoAte(agora));
    }
}

