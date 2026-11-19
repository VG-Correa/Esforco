using System.Windows;
using Esforco.Dominio;

namespace Esforco.Ui;

public partial class JanelaDePeriodo : Window
{
    public JanelaDePeriodo()
    {
        InitializeComponent();
        var hoje = DateTime.Today;
        DiaInicial.SelectedDate = hoje;
        DiaFinal.SelectedDate = hoje;
    }

    public DateTimeOffset Inicio { get; private set; }

    public DateTimeOffset Fim { get; private set; }

    private void Confirmar(object sender, RoutedEventArgs e)
    {
        Aviso.Text = "";
        try
        {
            var agora = DateTimeOffset.Now;
            (Inicio, Fim) = OpcaoOntem.IsChecked == true
                ? PeriodoLocal.Ontem(agora)
                : OpcaoSemana.IsChecked == true
                    ? PeriodoLocal.Semana(agora)
                    : OpcaoDatas.IsChecked == true
                        ? Datas()
                        : PeriodoLocal.Hoje(agora);
            DialogResult = true;
        }
        catch (ArgumentOutOfRangeException)
        {
            Aviso.Text = "A data final precisa ser a mesma ou vir depois da inicial.";
        }
        catch (InvalidOperationException ex)
        {
            Aviso.Text = ex.Message;
        }
    }

    private (DateTimeOffset Inicio, DateTimeOffset Fim) Datas()
    {
        if (DiaInicial.SelectedDate is not DateTime primeiro || DiaFinal.SelectedDate is not DateTime ultimo)
            throw new InvalidOperationException("Escolha as duas datas.");

        return PeriodoLocal.EntreDias(primeiro, ultimo);
    }
}
