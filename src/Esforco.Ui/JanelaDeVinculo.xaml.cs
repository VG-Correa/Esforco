using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Esforco.Aplicacao;
using Esforco.Dominio;

namespace Esforco.Ui;

/// <summary>
/// A janelinha que liga um programa, uma janela ou uma aba a um escritório e que guarda como a janela
/// conta o tempo. Ela só mostra e grava: não lê nem mexe em nenhuma outra janela.
/// </summary>
public partial class JanelaDeVinculo : Window
{
    public const string TituloDaJanela = "Escritório e contagem da janela";

    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("pt-BR");

    private readonly ServicoDeEsforco _servico;
    private readonly ServicoDeVinculos _vinculos;
    private readonly AlvoDeVinculo? _origem;
    private readonly AlcanceDoVinculo? _alcanceSugerido;
    private readonly List<ItemDeEscritorio> _escritorios = [];
    private FormularioDeVinculo? _formulario;
    private FormularioDeContagem? _contagem;
    private string? _escolhido;
    private string _diaPadrao = "";
    private string _horaPadrao = "";
    private bool _filtrando;
    private bool _modal;
    private bool _pronta;

    /// <summary>
    /// Origem é a janela da frente ou a faixa do calendário, guardada para o botão que troca
    /// a edição por um vínculo novo. A tela de vínculos salvos não tem origem.
    /// </summary>
    private JanelaDeVinculo(
        ServicoDeEsforco servico,
        ServicoDeVinculos vinculos,
        FormularioDeVinculo? formulario,
        AlvoDeVinculo? origem = null,
        AlcanceDoVinculo? alcanceSugerido = null)
    {
        InitializeComponent();
        Title = TituloDaJanela;
        _servico = servico;
        _vinculos = vinculos;
        _origem = origem;
        _alcanceSugerido = alcanceSugerido;
        MaxHeight = SystemParameters.WorkArea.Height;
        Loaded += (_, _) =>
        {
            ManterNaTela();
            if (PainelDoVinculo.Visibility != Visibility.Visible)
                return;

            MostrarEscolhido();
            Busca.Focus();
        };
        SizeChanged += (_, _) => ManterNaTela();
        Preencher(formulario);
        PreencherContagem(formulario is null ? null : origem);
    }

    public bool Salvou { get; private set; }

    /// <summary>
    /// Aberta pelo atalho. Sem alvo, o Esforço estava parado e nada foi lido.
    /// Se a janela da frente já tem vínculo, abre com ele.
    /// </summary>
    public static JanelaDeVinculo DoAtalho(ServicoDeEsforco servico, ServicoDeVinculos vinculos, AlvoDeVinculo? alvo) =>
        new(servico, vinculos, alvo is null ? null : vinculos.Formulario(alvo), alvo)
        {
            Topmost = true,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
        };

    /// <summary>
    /// Aberta pelo clique numa faixa do calendário. Se a faixa já tem vínculo, abre com ele.
    /// Na vista de programas a faixa junta todas as telas do programa, então o alcance sugerido
    /// para um vínculo novo é o programa inteiro, para cobrir a faixa toda.
    /// </summary>
    public static JanelaDeVinculo DoCalendario(
        ServicoDeEsforco servico,
        ServicoDeVinculos vinculos,
        AlvoDeVinculo alvo,
        AlcanceDoVinculo? alcance = null) =>
        new(servico, vinculos, vinculos.Formulario(alvo, alcance), alvo, alcance);

    public static JanelaDeVinculo DaEdicao(ServicoDeEsforco servico, ServicoDeVinculos vinculos, VinculoDeEscritorio vinculo) =>
        new(servico, vinculos, FormularioDeVinculo.DoVinculo(vinculo));

    /// <summary>
    /// Abre como diálogo da janela principal. Enquanto estiver aberta, o tempo segue contando
    /// para a janela que estava na frente, e a janelinha não vira registro.
    /// </summary>
    public async Task<bool> MostrarSegurando(Window? dona)
    {
        Owner = dona;
        WindowStartupLocation = dona is null ? WindowStartupLocation.CenterScreen : WindowStartupLocation.CenterOwner;
        await Task.Run(() => _servico.SegurarJanelaDaFrente(TituloDaJanela));
        try
        {
            _modal = true;
            ShowDialog();
        }
        finally
        {
            _servico.SoltarJanelaDaFrente();
        }

        return Salvou;
    }

    /// <summary>
    /// A janelinha cresce para baixo quando um campo a mais aparece. Se com isso ela passar do fim
    /// da tela, sobe o que faltar. Só mexe nela mesma, e só na tela principal, que é a única de que
    /// dá para saber a área útil sem chamar o Windows.
    /// </summary>
    private void ManterNaTela()
    {
        if (!IsLoaded || WindowState != WindowState.Normal || double.IsNaN(Left) || double.IsNaN(Top))
            return;

        var tela = new Rect(0, 0, SystemParameters.PrimaryScreenWidth, SystemParameters.PrimaryScreenHeight);
        if (!tela.Contains(new Point(Left + ActualWidth / 2, Top + ActualHeight / 2)))
            return;

        var area = SystemParameters.WorkArea;
        var topo = Math.Max(area.Top, Math.Min(Top, area.Bottom - ActualHeight));
        if (Math.Abs(topo - Top) > 0.5)
            Top = topo;
    }

    private void Preencher(FormularioDeVinculo? formulario)
    {
        _pronta = false;
        _formulario = formulario;
        if (formulario is null)
        {
            PainelParado.Visibility = Visibility.Visible;
            PainelDoVinculo.Visibility = Visibility.Collapsed;
            return;
        }

        var alvo = formulario.Alvo;
        var emEdicao = formulario.EmEdicao is not null;
        Cabecalho.Text = emEdicao
            ? "Vínculo salvo"
            : alvo.DoCalendario ? "Registro do calendário" : "O que está sendo registrado";
        TextoPrograma.Text = alvo.Programa;
        Mostrar(TextoTitulo, alvo.Titulo);
        Mostrar(TextoEndereco, alvo.Endereco);
        Mostrar(TextoPeriodo, alvo.DoCalendario ? Periodo(alvo.Inicio!.Value, alvo.Fim!.Value) : "");
        AvisoDescoberto.Visibility = alvo.Descoberto ? Visibility.Visible : Visibility.Collapsed;
        PreencherSituacao(formulario);

        CarregarEscritorios(formulario);
        CarregarCategorias(formulario);
        PreencherAlcance(formulario);
        PreencherValidade(formulario);
        Aviso.Text = "";
        _pronta = true;
        AjustarOpcoes();
    }

    /// <summary>
    /// Mostra como a janela da frente, ou a do registro clicado, conta o tempo: o que está salvo ou o padrão.
    /// A tela de vínculos salvos não tem janela por trás, então lá a escolha não aparece.
    /// </summary>
    private void PreencherContagem(AlvoDeVinculo? origem)
    {
        if (origem is null || origem.Programa.Trim().Length == 0)
        {
            _contagem = null;
            PainelDaContagem.Visibility = Visibility.Collapsed;
            return;
        }

        var contagem = _vinculos.FormularioDaContagem(origem.Programa, origem.Titulo);
        _contagem = null;
        TextoDoProgramaDaContagem.Text = $"o programa inteiro ({contagem.Programa})";
        ContagemPelaJanela.IsEnabled = contagem.PodeJanela;
        ContagemPelaJanela.IsChecked = contagem.Alcance == AlcanceDaContagem.Janela;
        ContagemPeloPrograma.IsChecked = contagem.Alcance == AlcanceDaContagem.Programa;
        MarcarModo(contagem.Modo);
        PainelDaContagem.Visibility = Visibility.Visible;
        _contagem = contagem;
        MostrarSituacaoDaContagem();
    }

    /// <summary>
    /// Ao trocar entre a janela e o programa inteiro, mostra o que já está salvo para aquele alcance.
    /// Sem nada salvo lá, a escolha marcada fica como a pessoa deixou.
    /// </summary>
    private void AoTrocarAlcanceDaContagem(object sender, RoutedEventArgs e)
    {
        if (_contagem is null)
            return;

        if (_contagem.ModoSalvoEm(AlcanceDaContagemEscolhido()) is ModoDeContagem salvo)
            MarcarModo(salvo);
        MostrarSituacaoDaContagem();
    }

    private void MostrarSituacaoDaContagem()
    {
        if (_contagem is null)
            return;

        var alcance = AlcanceDaContagemEscolhido();
        var daJanela = alcance == AlcanceDaContagem.Janela;
        string texto;
        if (_contagem.ModoSalvoEm(alcance) is not null)
            texto = daJanela ? "Escolha salva para esta janela." : "Escolha salva para o programa inteiro.";
        else if (daJanela && _contagem.ModoDoPrograma is not null)
            texto = "Nada salvo só para esta janela: hoje vale a escolha do programa inteiro.";
        else if (!daJanela && _contagem.ModoDaJanela is not null)
            texto = "Nada salvo para o programa inteiro: só esta janela tem escolha salva.";
        else if (_contagem.Reuniao)
            texto = "Padrão da reunião. A contagem pode ser salva sem escolher escritório.";
        else
            texto = "Padrão. A contagem pode ser salva sem escolher escritório.";

        SituacaoDaContagem.Text = texto;
    }

    private void MarcarModo(ModoDeContagem modo)
    {
        ContagemEmFoco.IsChecked = modo == ModoDeContagem.SoEmFoco;
        ContagemAberta.IsChecked = modo == ModoDeContagem.EnquantoAberta;
    }

    private AlcanceDaContagem AlcanceDaContagemEscolhido() =>
        ContagemPeloPrograma.IsChecked == true ? AlcanceDaContagem.Programa : AlcanceDaContagem.Janela;

    /// <summary>
    /// Grava a escolha de contagem e diz se algo mudou. Nada é gravado se a escolha é a que já valia.
    /// </summary>
    private bool SalvarContagem()
    {
        if (_contagem is null)
            return false;

        return _vinculos.SalvarContagem(
            _contagem.Programa,
            _contagem.Titulo,
            AlcanceDaContagemEscolhido(),
            ContagemAberta.IsChecked == true ? ModoDeContagem.EnquantoAberta : ModoDeContagem.SoEmFoco);
    }

    /// <summary>
    /// Diz quando a janelinha abriu com um vínculo que já existia, e o que salvar faz com ele.
    /// </summary>
    private void PreencherSituacao(FormularioDeVinculo formulario)
    {
        var doCalendario = _origem?.DoCalendario == true;
        var texto = "";
        if (formulario.EmEdicao is not null && _origem is not null)
        {
            texto = doCalendario
                ? "Registro já vinculado. Salvar altera o vínculo."
                : "Janela já vinculada. Salvar altera o vínculo.";
        }
        else if (formulario.Copia)
        {
            texto = "Campos do último vínculo desta janela, que já terminou. Salvar cria outro e o antigo fica como está.";
        }

        Situacao.Text = texto;
        PainelDaSituacao.Visibility = texto.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
        var podeTrocar = _origem is not null && (formulario.EmEdicao is not null || formulario.Copia);
        BotaoOutroVinculo.Content = formulario.Copia ? "Limpar os campos" : "Criar outro vínculo";
        BotaoOutroVinculo.Visibility = podeTrocar ? Visibility.Visible : Visibility.Collapsed;
    }

    private void CriarOutro(object sender, RoutedEventArgs e)
    {
        if (_origem is null)
            return;

        Busca.Text = "";
        Preencher(_vinculos.FormularioNovo(_origem, _alcanceSugerido));
        Busca.Focus();
    }

    private void CarregarEscritorios(FormularioDeVinculo formulario)
    {
        _escritorios.Clear();
        foreach (var escritorio in _servico.ListarEscritoriosComNome())
            _escritorios.Add(new ItemDeEscritorio(escritorio.Id, $"{escritorio.Nome}  ·  id {escritorio.Id}", escritorio.Nome));

        var salvo = formulario.Escritorio;
        if (salvo.Length > 0 && _escritorios.All(item => item.Id != salvo))
            _escritorios.Add(new ItemDeEscritorio(salvo, $"Escritório {salvo}", ""));

        _escolhido = salvo.Length > 0 ? salvo : null;
        _filtrando = true;
        try
        {
            ListaDeEscritorios.SelectedItem = null;
        }
        finally
        {
            _filtrando = false;
        }

        if (_escritorios.Count == 0)
        {
            Busca.IsEnabled = false;
            ListaDeEscritorios.Visibility = Visibility.Collapsed;
            SemEscritorios.Text = "Nenhum escritório tem nome ainda. Dê o nome no relatório de Escritórios: botão … do escritório, Editar nome.";
            SemEscritorios.Visibility = Visibility.Visible;
            return;
        }

        Filtrar();
    }

    private void CarregarCategorias(FormularioDeVinculo formulario)
    {
        var categorias = _vinculos.ListarCategorias();
        CampoCategoria.ItemsSource = categorias.Select(item => item.Nome).ToList();
        CampoCategoria.Text = formulario.CategoriaId is long id
            ? categorias.FirstOrDefault(item => item.Id == id)?.Nome ?? ""
            : "";
    }

    private void PreencherAlcance(FormularioDeVinculo formulario)
    {
        var alvo = formulario.Alvo;
        AlcancePrograma.Content = $"O programa inteiro ({alvo.Programa})";
        AlcanceJanela.IsEnabled = formulario.PodeJanela;
        AlcanceAba.IsEnabled = formulario.PodeAba;
        AlcanceAba.Content = alvo.Endereco.Trim().Length > 0
            ? "Esta aba, pelo título ou pelo endereço"
            : "Esta aba, pelo título";

        AlcancePrograma.IsChecked = formulario.Alcance == AlcanceDoVinculo.Programa;
        AlcanceJanela.IsChecked = formulario.Alcance == AlcanceDoVinculo.Janela;
        AlcanceAba.IsChecked = formulario.Alcance == AlcanceDoVinculo.Aba;
    }

    private void PreencherValidade(FormularioDeVinculo formulario)
    {
        var local = formulario.Inicio.ToLocalTime();
        DiaDoInicio.SelectedDate = local.Date;
        HoraDoInicio.Text = local.ToString("HH:mm", Cultura);
        _diaPadrao = local.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        _horaPadrao = HoraDoInicio.Text;

        var alvo = formulario.Alvo;
        PainelDoRegistro.Visibility = formulario.OfereceRegistro ? Visibility.Visible : Visibility.Collapsed;
        if (formulario.OfereceRegistro)
            SoRegistro.Content = $"Só este registro, {Periodo(alvo.Inicio!.Value, alvo.Fim!.Value)}";
        SoRegistro.IsChecked = formulario.SoRegistro;
        SempreRegistro.IsChecked = !formulario.SoRegistro;

        ValidadeSempre.IsChecked = formulario.Validade == ValidadeDoVinculo.Sempre;
        ValidadePrazo.IsChecked = formulario.Validade == ValidadeDoVinculo.Prazo;
        ValidadeInatividade.IsChecked = formulario.Validade == ValidadeDoVinculo.Inatividade;

        var tempo = formulario.Tempo;
        Tempo.SelectedIndex = (int)tempo.Opcao;
        UnidadeDoTempo.SelectedIndex = tempo.EmHoras ? 1 : 0;
        OutroTempo.Text = tempo.Opcao == OpcaoDeTempo.Outro
            ? tempo.Quantidade.ToString(CultureInfo.InvariantCulture)
            : "";
    }

    private void AoMudarOpcao(object sender, RoutedEventArgs e) => AjustarOpcoes();

    private void AoMudarTempo(object sender, SelectionChangedEventArgs e) => AjustarOpcoes();

    private void AjustarOpcoes()
    {
        if (!_pronta || _formulario is null)
            return;

        var soRegistro = PainelDoRegistro.Visibility == Visibility.Visible && SoRegistro.IsChecked == true;
        PainelDaValidade.Visibility = soRegistro ? Visibility.Collapsed : Visibility.Visible;
        PainelDoPrazo.Visibility = ValidadeSempre.IsChecked == true ? Visibility.Collapsed : Visibility.Visible;
        RotuloDoTempo.Text = ValidadeInatividade.IsChecked == true ? "Sem voltar por" : "Por";
        var outro = Tempo.SelectedIndex == 2 ? Visibility.Visible : Visibility.Collapsed;
        OutroTempo.Visibility = outro;
        UnidadeDoTempo.Visibility = outro;
    }

    private void AoBuscar(object sender, TextChangedEventArgs e)
    {
        if (!_pronta)
            return;

        Filtrar();
    }

    /// <summary>
    /// Só a escolha feita pela pessoa, com o mouse ou as setas, vira o escritório escolhido.
    /// A marcação que a busca faz sozinha não troca a escolha.
    /// </summary>
    private void AoEscolherEscritorio(object sender, SelectionChangedEventArgs e)
    {
        if (_filtrando)
            return;

        _escolhido = (ListaDeEscritorios.SelectedItem as ItemDeEscritorio)?.Id;
    }

    private void AoTeclarNaBusca(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Down && e.Key != Key.Up)
            return;

        var total = ListaDeEscritorios.Items.Count;
        if (total == 0)
            return;

        var indice = ListaDeEscritorios.SelectedIndex + (e.Key == Key.Down ? 1 : -1);
        ListaDeEscritorios.SelectedIndex = Math.Clamp(indice, 0, total - 1);
        MostrarEscolhido();
        e.Handled = true;
    }

    private void Filtrar()
    {
        var marcado = (ListaDeEscritorios.SelectedItem as ItemDeEscritorio)?.Id;
        var achados = BuscaDeEscritorio.Filtrar(_escritorios, Busca.Text, item => item.Rotulo).ToList();
        var id = BuscaDeEscritorio.Marcar(
            _escolhido,
            marcado,
            achados.Select(item => item.Id).ToList(),
            Busca.Text.Trim().Length > 0);
        _filtrando = true;
        try
        {
            ListaDeEscritorios.ItemsSource = achados;
            ListaDeEscritorios.SelectedItem = id is null ? null : achados.FirstOrDefault(item => item.Id == id);
        }
        finally
        {
            _filtrando = false;
        }

        MostrarEscolhido();
    }

    private void MostrarEscolhido()
    {
        if (ListaDeEscritorios.SelectedItem is object item)
            ListaDeEscritorios.ScrollIntoView(item);
    }

    private void Confirmar(object sender, RoutedEventArgs e)
    {
        if (_formulario is null)
        {
            Close();
            return;
        }

        if (ListaDeEscritorios.SelectedItem is not ItemDeEscritorio escritorio)
        {
            // Sem escritório, só a contagem é salva, e só se a pessoa mudou alguma coisa nela.
            if (!SalvarContagem())
            {
                Aviso.Text = _contagem is null
                    ? "Escolha o escritório."
                    : "Escolha o escritório, ou mude a contagem do tempo para salvar só ela.";
                return;
            }

            Salvou = true;
            Fechar();
            return;
        }

        var soRegistro = PainelDoRegistro.Visibility == Visibility.Visible && SoRegistro.IsChecked == true;
        var validade = soRegistro || ValidadeSempre.IsChecked == true
            ? ValidadeDoVinculo.Sempre
            : ValidadePrazo.IsChecked == true ? ValidadeDoVinculo.Prazo : ValidadeDoVinculo.Inatividade;
        var inicio = _formulario.Inicio;
        TimeSpan? duracao = null;
        if (validade != ValidadeDoVinculo.Sempre)
        {
            if (!LerInicio(out inicio) || !LerDuracao(out var lida))
                return;
            duracao = lida;
        }

        var alcance = AlcanceAba.IsChecked == true
            ? AlcanceDoVinculo.Aba
            : AlcanceJanela.IsChecked == true ? AlcanceDoVinculo.Janela : AlcanceDoVinculo.Programa;
        var pedido = new PedidoDeVinculo(
            escritorio.Id,
            null,
            alcance,
            _formulario.Alvo,
            validade,
            inicio,
            duracao,
            soRegistro,
            CampoCategoria.Text ?? "");
        var resultado = _servico.SalvarVinculo(_formulario.EmEdicao, pedido);
        if (resultado.Aviso is not null)
        {
            Aviso.Text = resultado.Aviso;
            return;
        }

        SalvarContagem();
        Salvou = true;
        Fechar();
    }

    private void Cancelar(object sender, RoutedEventArgs e) => Fechar();

    private void AoTeclar(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape || CampoCategoria.IsDropDownOpen || DiaDoInicio.IsDropDownOpen)
            return;

        e.Handled = true;
        Fechar();
    }

    private void Fechar()
    {
        if (_modal)
            DialogResult = Salvou;
        else
            Close();
    }

    private bool LerInicio(out DateTimeOffset inicio)
    {
        inicio = _formulario!.Inicio;
        if (DiaDoInicio.SelectedDate is not DateTime dia)
        {
            Aviso.Text = "Escolha o dia em que o vínculo começa.";
            return false;
        }

        var hora = HoraDoInicio.Text.Trim();
        if (dia.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) == _diaPadrao && hora == _horaPadrao)
            return true;

        string[] formatos = ["H:mm", "HH:mm", "H\\hmm", "HHmm"];
        if (!DateTime.TryParseExact(hora, formatos, CultureInfo.InvariantCulture, DateTimeStyles.None, out var lida))
        {
            Aviso.Text = "Escreva a hora como 14:30.";
            return false;
        }

        inicio = new DateTimeOffset(DateTime.SpecifyKind(dia.Date + lida.TimeOfDay, DateTimeKind.Local));
        return true;
    }

    private bool LerDuracao(out TimeSpan duracao)
    {
        duracao = Tempo.SelectedIndex switch
        {
            0 => TimeSpan.FromMinutes(30),
            1 => TimeSpan.FromHours(1),
            _ => TimeSpan.Zero,
        };
        if (Tempo.SelectedIndex != 2)
            return true;

        if (!int.TryParse(OutroTempo.Text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var numero) || numero <= 0)
        {
            Aviso.Text = "Informe o tempo em números, como 45.";
            return false;
        }

        duracao = UnidadeDoTempo.SelectedIndex == 1 ? TimeSpan.FromHours(numero) : TimeSpan.FromMinutes(numero);
        return true;
    }

    private static void Mostrar(TextBlock campo, string texto)
    {
        campo.Text = texto;
        campo.ToolTip = texto.Length > 60 ? texto : null;
        campo.Visibility = texto.Trim().Length == 0 ? Visibility.Collapsed : Visibility.Visible;
    }

    private static string Periodo(DateTimeOffset inicio, DateTimeOffset fim)
    {
        var de = inicio.ToLocalTime();
        var ate = fim.ToLocalTime();
        return de.Date == ate.Date
            ? $"das {de:HH:mm} às {ate:HH:mm} de {de:dd/MM/yyyy}"
            : $"de {de:dd/MM/yyyy HH:mm} a {ate:dd/MM/yyyy HH:mm}";
    }

    private sealed record ItemDeEscritorio(string Id, string Rotulo, string Nome);
}
