using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Esforco.Aplicacao;
using Esforco.Dominio;

namespace Esforco.Ui;

/// <summary>
/// A janelinha que liga um programa, uma janela ou uma aba a um escritório.
/// Ela só mostra e grava: não lê nem mexe em nenhuma outra janela.
/// </summary>
public partial class JanelaDeVinculo : Window
{
    public const string TituloDaJanela = "Vincular a um escritório";

    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("pt-BR");

    private readonly ServicoDeEsforco _servico;
    private readonly ServicoDeVinculos _vinculos;
    private readonly AlvoDeVinculo? _alvo;
    private readonly VinculoDeEscritorio? _emEdicao;
    private readonly DateTimeOffset _inicioPadrao;
    private readonly AlcanceDoVinculo? _alcancePadrao;
    private readonly List<ItemDeEscritorio> _escritorios = [];
    private string _diaPadrao = "";
    private string _horaPadrao = "";
    private bool _modal;
    private bool _pronta;

    private JanelaDeVinculo(
        ServicoDeEsforco servico,
        ServicoDeVinculos vinculos,
        AlvoDeVinculo? alvo,
        VinculoDeEscritorio? emEdicao,
        DateTimeOffset inicioPadrao,
        AlcanceDoVinculo? alcancePadrao = null)
    {
        InitializeComponent();
        _alcancePadrao = alcancePadrao;
        Title = TituloDaJanela;
        _servico = servico;
        _vinculos = vinculos;
        _alvo = alvo;
        _emEdicao = emEdicao;
        _inicioPadrao = inicioPadrao;
        Loaded += (_, _) =>
        {
            if (PainelDoVinculo.Visibility == Visibility.Visible)
                Busca.Focus();
        };
        Preencher();
        _pronta = true;
        AjustarOpcoes();
    }

    public bool Salvou { get; private set; }

    /// <summary>
    /// Aberta pelo atalho. Sem alvo, o Esforço estava parado e nada foi lido.
    /// </summary>
    public static JanelaDeVinculo DoAtalho(ServicoDeEsforco servico, ServicoDeVinculos vinculos, AlvoDeVinculo? alvo) =>
        new(servico, vinculos, alvo, null, DateTimeOffset.Now)
        {
            Topmost = true,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
        };

    /// <summary>
    /// Aberta pelo clique numa faixa do calendário. Na vista de programas a faixa junta todas as
    /// telas do programa, então o alcance sugerido é o programa inteiro, para cobrir a faixa toda.
    /// </summary>
    public static JanelaDeVinculo DoCalendario(
        ServicoDeEsforco servico,
        ServicoDeVinculos vinculos,
        AlvoDeVinculo alvo,
        AlcanceDoVinculo? alcance = null) =>
        new(servico, vinculos, alvo, null, alvo.Inicio ?? DateTimeOffset.Now, alcance);

    public static JanelaDeVinculo DaEdicao(ServicoDeEsforco servico, ServicoDeVinculos vinculos, VinculoDeEscritorio vinculo)
    {
        var alvo = new AlvoDeVinculo(
            vinculo.Programa,
            vinculo.Titulo,
            vinculo.Endereco,
            vinculo.SoRegistro ? vinculo.Inicio : null,
            vinculo.SoRegistro ? vinculo.Inicio + (vinculo.Duracao ?? TimeSpan.Zero) : null);
        return new JanelaDeVinculo(servico, vinculos, alvo, vinculo, vinculo.Inicio);
    }

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

    private void Preencher()
    {
        if (_alvo is null)
        {
            PainelParado.Visibility = Visibility.Visible;
            PainelDoVinculo.Visibility = Visibility.Collapsed;
            return;
        }

        Cabecalho.Text = _emEdicao is not null
            ? "Vínculo salvo"
            : _alvo.DoCalendario ? "Registro do calendário" : "O que está sendo registrado";
        TextoPrograma.Text = _alvo.Programa;
        Mostrar(TextoTitulo, _alvo.Titulo);
        Mostrar(TextoEndereco, _alvo.Endereco);
        Mostrar(TextoPeriodo, _alvo.DoCalendario ? Periodo(_alvo.Inicio!.Value, _alvo.Fim!.Value) : "");
        AvisoDescoberto.Visibility = _alvo.Descoberto ? Visibility.Visible : Visibility.Collapsed;

        CarregarEscritorios();
        CarregarCategorias();
        PreencherAlcance();
        PreencherValidade();
        if (_emEdicao is not null)
            BotaoSalvar.Content = "Salvar";
    }

    private void CarregarEscritorios()
    {
        _escritorios.Clear();
        foreach (var escritorio in _servico.ListarEscritoriosComNome())
            _escritorios.Add(new ItemDeEscritorio(escritorio.Id, $"{escritorio.Nome}  ·  id {escritorio.Id}", escritorio.Nome));

        if (_emEdicao is not null && _escritorios.All(item => item.Id != _emEdicao.Escritorio))
            _escritorios.Add(new ItemDeEscritorio(_emEdicao.Escritorio, $"Escritório {_emEdicao.Escritorio}", ""));

        if (_escritorios.Count == 0)
        {
            Busca.IsEnabled = false;
            ListaDeEscritorios.Visibility = Visibility.Collapsed;
            SemEscritorios.Text = "Nenhum escritório tem nome ainda. Dê o nome no relatório de Escritórios: botão … do escritório, Editar nome.";
            SemEscritorios.Visibility = Visibility.Visible;
            return;
        }

        Filtrar();
        if (_emEdicao is not null)
            ListaDeEscritorios.SelectedItem = _escritorios.FirstOrDefault(item => item.Id == _emEdicao.Escritorio);
    }

    private void CarregarCategorias()
    {
        var categorias = _vinculos.ListarCategorias();
        CampoCategoria.ItemsSource = categorias.Select(item => item.Nome).ToList();
        if (_emEdicao?.CategoriaId is long id)
            CampoCategoria.Text = categorias.FirstOrDefault(item => item.Id == id)?.Nome ?? "";
    }

    private void PreencherAlcance()
    {
        var temTitulo = _alvo!.Titulo.Trim().Length > 0;
        var temEndereco = _alvo.Endereco.Trim().Length > 0;
        AlcancePrograma.Content = $"O programa inteiro ({_alvo.Programa})";
        AlcanceJanela.IsEnabled = temTitulo;
        AlcanceAba.IsEnabled = temTitulo || temEndereco;
        AlcanceAba.Content = temEndereco
            ? "Esta aba, pelo título ou pelo endereço"
            : "Esta aba, pelo título";

        var alcance = _emEdicao?.Alcance
            ?? _alcancePadrao
            ?? (temEndereco ? AlcanceDoVinculo.Aba : temTitulo ? AlcanceDoVinculo.Janela : AlcanceDoVinculo.Programa);
        if ((alcance == AlcanceDoVinculo.Janela && !temTitulo) || (alcance == AlcanceDoVinculo.Aba && !temTitulo && !temEndereco))
            alcance = AlcanceDoVinculo.Programa;
        AlcancePrograma.IsChecked = alcance == AlcanceDoVinculo.Programa;
        AlcanceJanela.IsChecked = alcance == AlcanceDoVinculo.Janela;
        AlcanceAba.IsChecked = alcance == AlcanceDoVinculo.Aba;
    }

    private void PreencherValidade()
    {
        var local = _inicioPadrao.ToLocalTime();
        DiaDoInicio.SelectedDate = local.Date;
        HoraDoInicio.Text = local.ToString("HH:mm", Cultura);
        _diaPadrao = local.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        _horaPadrao = HoraDoInicio.Text;

        if (_alvo!.DoCalendario)
        {
            PainelDoRegistro.Visibility = Visibility.Visible;
            SoRegistro.Content = $"Só este registro, {Periodo(_alvo.Inicio!.Value, _alvo.Fim!.Value)}";
            if (_emEdicao is not null)
                return;
        }

        if (_emEdicao is null || _emEdicao.SoRegistro)
            return;

        ValidadePrazo.IsChecked = _emEdicao.Validade == ValidadeDoVinculo.Prazo;
        ValidadeInatividade.IsChecked = _emEdicao.Validade == ValidadeDoVinculo.Inatividade;
        if (_emEdicao.Duracao is not TimeSpan duracao)
            return;

        if (duracao == TimeSpan.FromMinutes(30))
        {
            Tempo.SelectedIndex = 0;
        }
        else if (duracao == TimeSpan.FromHours(1))
        {
            Tempo.SelectedIndex = 1;
        }
        else
        {
            Tempo.SelectedIndex = 2;
            var emHoras = duracao.TotalMinutes >= 60 && duracao.TotalMinutes % 60 == 0;
            UnidadeDoTempo.SelectedIndex = emHoras ? 1 : 0;
            OutroTempo.Text = Math.Max(1, Math.Round(emHoras ? duracao.TotalHours : duracao.TotalMinutes))
                .ToString(CultureInfo.InvariantCulture);
        }
    }

    private void AoMudarOpcao(object sender, RoutedEventArgs e) => AjustarOpcoes();

    private void AoMudarTempo(object sender, SelectionChangedEventArgs e) => AjustarOpcoes();

    private void AjustarOpcoes()
    {
        if (!_pronta || _alvo is null)
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
        if (Busca.Text.Trim().Length > 0 && ListaDeEscritorios.Items.Count > 0)
            ListaDeEscritorios.SelectedIndex = 0;
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
        ListaDeEscritorios.ScrollIntoView(ListaDeEscritorios.SelectedItem);
        e.Handled = true;
    }

    private void Filtrar()
    {
        var escolhido = ListaDeEscritorios.SelectedItem as ItemDeEscritorio;
        var texto = Busca.Text.Trim();
        var comparador = Cultura.CompareInfo;
        var achados = texto.Length == 0
            ? _escritorios
            : _escritorios
                .Where(item => comparador.IndexOf(
                    item.Rotulo,
                    texto,
                    CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0)
                .ToList();
        ListaDeEscritorios.ItemsSource = achados;
        if (escolhido is not null && achados.Contains(escolhido))
            ListaDeEscritorios.SelectedItem = escolhido;
    }

    private void Confirmar(object sender, RoutedEventArgs e)
    {
        if (_alvo is null)
        {
            Close();
            return;
        }

        if (ListaDeEscritorios.SelectedItem is not ItemDeEscritorio escritorio)
        {
            Aviso.Text = "Escolha o escritório.";
            return;
        }

        var soRegistro = PainelDoRegistro.Visibility == Visibility.Visible && SoRegistro.IsChecked == true;
        var validade = soRegistro || ValidadeSempre.IsChecked == true
            ? ValidadeDoVinculo.Sempre
            : ValidadePrazo.IsChecked == true ? ValidadeDoVinculo.Prazo : ValidadeDoVinculo.Inatividade;
        var inicio = _inicioPadrao;
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
            _alvo,
            validade,
            inicio,
            duracao,
            soRegistro,
            CampoCategoria.Text ?? "");
        var resultado = _emEdicao is null
            ? _servico.Vincular(pedido)
            : _vinculos.Editar(_emEdicao.Id, pedido);
        if (resultado.Aviso is not null)
        {
            Aviso.Text = resultado.Aviso;
            return;
        }

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
        inicio = _inicioPadrao;
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
