using System.Windows;
using System.Windows.Controls;
using Esforco.Aplicacao;
using Esforco.Dominio;

namespace Esforco.Ui;

public partial class JanelaDeOcultos : Window
{
    private readonly ServicoDeEsforco _servico;

    public JanelaDeOcultos(ServicoDeEsforco servico)
    {
        InitializeComponent();
        _servico = servico;
        Recarregar();
    }

    private void Adicionar(object sender, RoutedEventArgs e)
    {
        var aviso = _servico.OcultarNoRelatorio(OpcaoId.IsChecked == true, Texto.Text);
        Aviso.Text = aviso ?? "";
        if (aviso is not null)
            return;

        Texto.Text = "";
        Recarregar();
    }

    private void Tirar(object sender, RoutedEventArgs e)
    {
        if (sender is not Button botao || botao.Tag is not OcultacaoDeEscritorio ocultacao)
            return;

        _servico.ReexibirNoRelatorio(ocultacao);
        Aviso.Text = "";
        Recarregar();
    }

    private void Recarregar()
    {
        var itens = _servico.ListarOcultacoes()
            .Select(item => new Item(item, item.PorId ? $"Id {item.Texto}" : item.Texto))
            .ToList();
        Lista.ItemsSource = itens;
        Vazio.Visibility = itens.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private sealed record Item(OcultacaoDeEscritorio Ocultacao, string Rotulo);
}
