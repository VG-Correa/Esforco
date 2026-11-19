using System.Windows;
using Esforco.Dominio;

namespace Esforco.Ui;

public partial class JanelaDeAtalho : Window
{
    private readonly AtalhoDaJanela _atalho;

    public JanelaDeAtalho(AtalhoDaJanela atalho)
    {
        InitializeComponent();
        _atalho = atalho;
        var teclas = new List<string>();
        for (var letra = 'A'; letra <= 'Z'; letra++)
            teclas.Add(letra.ToString());
        for (var digito = '0'; digito <= '9'; digito++)
            teclas.Add(digito.ToString());
        for (var funcao = 1; funcao <= 12; funcao++)
            teclas.Add($"F{funcao}");
        Tecla.ItemsSource = teclas;
        Mostrar(_atalho.Atual);
        DizerSituacao();
    }

    private void Salvar(object sender, RoutedEventArgs e)
    {
        var tecla = Tecla.SelectedItem as string;
        var aviso = AtalhoDeTeclado.Impedir(UsarCtrl.IsChecked == true, UsarAlt.IsChecked == true, UsarWin.IsChecked == true, tecla);
        if (aviso is null)
        {
            var novo = AtalhoDeTeclado.Criar(
                UsarCtrl.IsChecked == true,
                UsarAlt.IsChecked == true,
                UsarShift.IsChecked == true,
                UsarWin.IsChecked == true,
                tecla)!;
            aviso = _atalho.Trocar(novo);
        }

        Aviso.Text = aviso ?? "";
        DizerSituacao();
        if (aviso is null)
            DialogResult = true;
    }

    private void UsarPadrao(object sender, RoutedEventArgs e)
    {
        Mostrar(AtalhoDeTeclado.Padrao);
        Aviso.Text = "";
    }

    private void Mostrar(AtalhoDeTeclado atalho)
    {
        UsarCtrl.IsChecked = atalho.Ctrl;
        UsarAlt.IsChecked = atalho.Alt;
        UsarShift.IsChecked = atalho.Shift;
        UsarWin.IsChecked = atalho.Win;
        Tecla.SelectedItem = atalho.Tecla;
    }

    private void DizerSituacao()
    {
        Situacao.Text = _atalho.Registrado
            ? $"Em uso agora: {_atalho.Atual}."
            : $"{_atalho.Atual} não está valendo: o Windows recusou essa combinação, porque outro programa já a usa.";
    }
}
