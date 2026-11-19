using System.Windows;
using System.Windows.Controls;
using Esforco.Aplicacao;
using Esforco.Dominio;

namespace Esforco.Ui;

public partial class JanelaDeVinculos : Window
{
    private readonly ServicoDeEsforco _servico;
    private readonly ServicoDeVinculos _vinculos;
    private bool _editando;

    public JanelaDeVinculos(ServicoDeEsforco servico, ServicoDeVinculos vinculos)
    {
        InitializeComponent();
        _servico = servico;
        _vinculos = vinculos;
        Recarregar();
    }

    private async void Editar(object sender, RoutedEventArgs e)
    {
        if (_editando || sender is not Button botao || botao.Tag is not Item item)
            return;

        _editando = true;
        try
        {
            Aviso.Text = "";
            var janela = JanelaDeVinculo.DaEdicao(_servico, _vinculos, item.Vinculo);
            await janela.MostrarSegurando(this);
            Recarregar();
        }
        finally
        {
            _editando = false;
        }
    }

    private void Encerrar(object sender, RoutedEventArgs e)
    {
        if (sender is not Button botao || botao.Tag is not Item item)
            return;

        _vinculos.EncerrarAgora(item.Vinculo.Id);
        Aviso.Text = "";
        Recarregar();
    }

    private void Excluir(object sender, RoutedEventArgs e)
    {
        if (sender is not Button botao || botao.Tag is not Item item)
            return;

        var resposta = MessageBox.Show(
            this,
            $"Excluir o vínculo de {item.Programa} com {item.Escritorio}? O tempo dele sai de Outros trabalhos. Os registros continuam como estão.",
            "Esforço",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning);
        if (resposta != MessageBoxResult.OK)
            return;

        _vinculos.Excluir(item.Vinculo.Id);
        Aviso.Text = "";
        Recarregar();
    }

    private void Recarregar()
    {
        var itens = _servico.ConsultarVinculos().Select(salvo => new Item(salvo)).ToList();
        Lista.ItemsSource = itens;
        Vazio.Visibility = itens.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private sealed class Item(VinculoSalvo salvo)
    {
        public VinculoDeEscritorio Vinculo { get; } = salvo.Vinculo;

        public string Escritorio { get; } = salvo.Escritorio;

        public string Categoria { get; } = salvo.Categoria.Length == 0 ? CadastroDeCategorias.SemCategoria : salvo.Categoria;

        public string Programa { get; } = salvo.Vinculo.Programa;

        public string Alcance { get; } = salvo.Alcance;

        public string Validade { get; } = salvo.Validade;

        public bool Inativo { get; } = !salvo.Ativo && !salvo.Vinculo.SoRegistro;

        public bool PodeEncerrar { get; } = salvo.Ativo && !salvo.Vinculo.SoRegistro;

        public double Opacidade { get; } = salvo.Ativo || salvo.Vinculo.SoRegistro ? 1 : 0.62;
    }
}
