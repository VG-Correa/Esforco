using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Esforco.Aplicacao;
using Esforco.Dominio;

namespace Esforco.Ui;

/// <summary>
/// Cria, altera ou exclui um registro manual. Não lê nenhuma janela: funciona com o Esforço parado.
/// </summary>
public partial class JanelaDeRegistroManual : Window
{
    private static readonly Brush TintaDoResumo = Tinta(0x7A, 0x75, 0x6C);
    private static readonly Brush TintaDoAviso = Tinta(0x8A, 0x3B, 0x32);

    private readonly ServicoDeEsforco _servico;
    private readonly ServicoDeVinculos _vinculos;
    private readonly RegistroManual? _registro;
    private readonly List<ItemDeEscritorio> _escritorios = [];
    private string? _escolhido;
    private bool _filtrando;
    private bool _pronta;
    private bool _confirmandoExclusao;

    private JanelaDeRegistroManual(
        ServicoDeEsforco servico,
        ServicoDeVinculos vinculos,
        RegistroManual? registro,
        DateTime dia,
        int inicioMinuto,
        int fimMinuto,
        bool terminaNoDiaSeguinte)
    {
        InitializeComponent();
        _servico = servico;
        _vinculos = vinculos;
        _registro = registro;

        Dia.SelectedDate = dia.Date;
        Inicio.Text = HoraDoDia.Escrever(inicioMinuto);
        Fim.Text = HoraDoDia.Escrever(fimMinuto);
        TerminaNoDiaSeguinte.IsChecked = terminaNoDiaSeguinte;

        var categorias = _vinculos.ListarCategorias();
        CampoCategoria.ItemsSource = categorias.Select(item => item.Nome).ToList();
        if (registro is not null)
        {
            Cabecalho.Text = "Alterar registro manual";
            BotaoExcluir.Visibility = Visibility.Visible;
            Descricao.Text = registro.Descricao;
            CampoCategoria.Text = registro.CategoriaId is long id
                ? categorias.FirstOrDefault(item => item.Id == id)?.Nome ?? ""
                : "";
        }

        CarregarEscritorios(registro?.Escritorio ?? "");
        _pronta = true;
        MostrarResumo();
        Loaded += (_, _) =>
        {
            if (Busca.IsEnabled)
                Busca.Focus();
        };
    }

    public bool Salvou { get; private set; }

    /// <summary>
    /// Registro novo. Fim em 1440 é a meia-noite: abre como 00:00 do dia seguinte.
    /// </summary>
    public static JanelaDeRegistroManual Novo(
        ServicoDeEsforco servico,
        ServicoDeVinculos vinculos,
        DateTime dia,
        int inicioMinuto,
        int fimMinuto) =>
        new(servico, vinculos, null, dia, inicioMinuto, fimMinuto, fimMinuto >= 24 * 60);

    public static JanelaDeRegistroManual Alterar(
        ServicoDeEsforco servico,
        ServicoDeVinculos vinculos,
        RegistroManual registro)
    {
        ArgumentNullException.ThrowIfNull(registro);
        var inicio = registro.Inicio.ToLocalTime();
        var fim = registro.Fim.ToLocalTime();
        return new JanelaDeRegistroManual(
            servico,
            vinculos,
            registro,
            inicio.Date,
            inicio.Hour * 60 + inicio.Minute,
            fim.Hour * 60 + fim.Minute,
            fim.Date > inicio.Date);
    }

    private void CarregarEscritorios(string salvo)
    {
        _escritorios.Clear();
        foreach (var escritorio in _servico.ListarEscritoriosComNome())
            _escritorios.Add(new ItemDeEscritorio(escritorio.Id, $"{escritorio.Nome}  ·  id {escritorio.Id}", escritorio.Nome));

        if (salvo.Length > 0 && _escritorios.All(item => item.Id != salvo))
            _escritorios.Add(new ItemDeEscritorio(salvo, $"Escritório {salvo}", ""));

        _escolhido = salvo.Length > 0 ? salvo : null;
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

    private void AoBuscar(object sender, TextChangedEventArgs e)
    {
        if (_pronta)
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
        Aviso.Text = "";
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

    private void AoMudarDia(object? sender, SelectionChangedEventArgs e) => MostrarResumo();

    private void AoMudarHorario(object sender, TextChangedEventArgs e) => MostrarResumo();

    private void AoMudarOpcao(object sender, RoutedEventArgs e) => MostrarResumo();

    private PeriodoDeRegistro Periodo() =>
        CadastroDeRegistrosManuais.LerPeriodo(
            Dia.SelectedDate,
            Inicio.Text,
            Fim.Text,
            TerminaNoDiaSeguinte.IsChecked == true);

    /// <summary>
    /// Abaixo do horário: a duração e o que vale avisar sem impedir, ou o que está errado no horário.
    /// </summary>
    private void MostrarResumo()
    {
        if (!_pronta)
            return;

        DesfazerConfirmacao();
        var periodo = Periodo();
        if (!periodo.Valido)
        {
            Resumo.Foreground = TintaDoAviso;
            Resumo.Text = periodo.Aviso ?? "";
            return;
        }

        var inicio = periodo.Inicio!.Value;
        var fim = periodo.Fim!.Value;
        var partes = new List<string> { "Duração: " + CadastroDeRegistrosManuais.Duracao(fim - inicio) + "." };
        partes.AddRange(CadastroDeRegistrosManuais.Notas(inicio, fim, DateTimeOffset.Now));
        Resumo.Foreground = TintaDoResumo;
        Resumo.Text = string.Join(" ", partes);
    }

    private void Confirmar(object sender, RoutedEventArgs e)
    {
        DesfazerConfirmacao();
        var periodo = Periodo();
        if (!periodo.Valido)
        {
            Aviso.Text = periodo.Aviso ?? "";
            return;
        }

        if (ListaDeEscritorios.SelectedItem is not ItemDeEscritorio escritorio)
        {
            Aviso.Text = "Escolha o escritório.";
            return;
        }

        var pedido = new PedidoDeRegistroManual(
            escritorio.Id,
            CampoCategoria.Text ?? "",
            Descricao.Text ?? "",
            periodo.Inicio!.Value,
            periodo.Fim!.Value);
        var resultado = _registro is null
            ? _vinculos.CriarRegistroManual(pedido)
            : _vinculos.EditarRegistroManual(_registro.Id, pedido);
        if (resultado.Aviso is not null)
        {
            Aviso.Text = resultado.Aviso;
            return;
        }

        Fechar(true);
    }

    /// <summary>
    /// A exclusão pede um segundo clique no mesmo botão, sem abrir outra janela.
    /// </summary>
    private void Excluir(object sender, RoutedEventArgs e)
    {
        if (_registro is null)
            return;

        if (!_confirmandoExclusao)
        {
            _confirmandoExclusao = true;
            BotaoExcluir.Content = "Excluir mesmo";
            Aviso.Text = "Excluir apaga este registro e o tempo dele sai dos totais. Clique em Excluir mesmo para confirmar.";
            return;
        }

        _vinculos.ExcluirRegistroManual(_registro.Id);
        Fechar(true);
    }

    private void DesfazerConfirmacao()
    {
        if (!_confirmandoExclusao)
            return;

        _confirmandoExclusao = false;
        BotaoExcluir.Content = "Excluir";
        Aviso.Text = "";
    }

    private void Cancelar(object sender, RoutedEventArgs e) => Fechar(false);

    private void AoTeclar(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape || CampoCategoria.IsDropDownOpen || Dia.IsDropDownOpen)
            return;

        e.Handled = true;
        Fechar(false);
    }

    private void Fechar(bool salvou)
    {
        Salvou = salvou;
        Close();
    }

    private static Brush Tinta(byte r, byte g, byte b)
    {
        var tinta = new SolidColorBrush(Color.FromRgb(r, g, b));
        tinta.Freeze();
        return tinta;
    }

    private sealed record ItemDeEscritorio(string Id, string Rotulo, string Nome);
}
