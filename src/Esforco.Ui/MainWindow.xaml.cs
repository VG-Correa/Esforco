using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace Esforco.Ui;

public partial class MainWindow : Window
{
    private readonly ModeloDaJanela _modelo;
    private readonly DispatcherTimer _timer;
    private readonly AtalhoDaJanela _atalho;

    public MainWindow(ModeloDaJanela modelo, AtalhoDaJanela atalho)
    {
        InitializeComponent();
        _modelo = modelo;
        _atalho = atalho;
        DataContext = modelo;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) =>
        {
            // Minimizada não há tela para atualizar. A leitura da janela da frente segue em PulsoDeLeitura.
            if (WindowState == WindowState.Minimized)
                return;

            _modelo.AtualizarDuranteLeitura();
            if (RolagemDoCalendario.IsVisible)
                InvalidarAgora(RolagemDoCalendario);
        };
        StateChanged += (_, _) =>
        {
            if (WindowState != WindowState.Minimized)
                _modelo.AtualizarDuranteLeitura();
        };
        _modelo.Atualizar();
        _timer.Start();
        EspacoDaBarra.Width = new GridLength(SystemParameters.VerticalScrollBarWidth);
        SourceInitialized += (_, _) => RegistrarAtalho();
        Closed += (_, _) =>
        {
            _timer.Stop();
            _atalho.Desligar();
        };
    }

    /// <summary>
    /// Pede ao Windows para reservar a combinação do atalho de vínculo para esta janela.
    /// É só o registro de atalho: nenhuma outra tecla passa pelo Esforço.
    /// </summary>
    private void RegistrarAtalho()
    {
        var janela = new WindowInteropHelper(this).Handle;
        if (janela == IntPtr.Zero)
            return;

        _atalho.Ligar(janela, _modelo.AbrirVinculoPeloAtalho);
    }

    private bool _gradeRolada;

    private void RolagemDoCalendario_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (_gradeRolada || sender is not ScrollViewer scroll)
            return;
        if (scroll.ExtentHeight < TrilhaDeHorario.AlturaDaHora * 12)
            return;

        _gradeRolada = true;
        scroll.ScrollToVerticalOffset(8 * TrilhaDeHorario.AlturaDaHora);
    }

    private static void InvalidarAgora(DependencyObject raiz)
    {
        var total = VisualTreeHelper.GetChildrenCount(raiz);
        for (var i = 0; i < total; i++)
        {
            var filho = VisualTreeHelper.GetChild(raiz, i);
            if (filho is TrilhaDeHorario trilha)
                trilha.InvalidarAgora();
            else
                InvalidarAgora(filho);
        }
    }

    private void AbrirMenu(object sender, RoutedEventArgs e)
    {
        if (sender is not Button botao || botao.ContextMenu is not ContextMenu menu)
            return;

        menu.PlacementTarget = botao;
        menu.DataContext = botao.DataContext;
        menu.IsOpen = true;
        e.Handled = true;
    }
}
