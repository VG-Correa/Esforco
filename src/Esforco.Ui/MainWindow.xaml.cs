using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Esforco.Ui;

public partial class MainWindow : Window
{
    private readonly ModeloDaJanela _modelo;
    private readonly DispatcherTimer _timer;

    public MainWindow(ModeloDaJanela modelo)
    {
        InitializeComponent();
        _modelo = modelo;
        DataContext = modelo;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) =>
        {
            _modelo.AtualizarDuranteLeitura();
            if (RolagemDoCalendario.IsVisible)
                InvalidarAgora(RolagemDoCalendario);
        };
        _modelo.Atualizar();
        _timer.Start();
        EspacoDaBarra.Width = new GridLength(SystemParameters.VerticalScrollBarWidth);
        Closed += (_, _) => _timer.Stop();
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
