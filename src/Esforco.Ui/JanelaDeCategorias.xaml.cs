using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Esforco.Aplicacao;
using Esforco.Dominio;

namespace Esforco.Ui;

public partial class JanelaDeCategorias : Window
{
    private readonly ServicoDeVinculos _vinculos;
    private Categoria? _excluindo;

    public JanelaDeCategorias(ServicoDeVinculos vinculos)
    {
        InitializeComponent();
        _vinculos = vinculos;
        Recarregar();
        Loaded += (_, _) => Nova.Focus();
    }

    private void Criar(object sender, RoutedEventArgs e)
    {
        var resultado = _vinculos.CriarCategoria(Nova.Text);
        Aviso.Text = resultado.Aviso ?? "";
        if (resultado.Aviso is not null)
            return;

        Nova.Text = "";
        FecharExclusao();
        Recarregar();
    }

    private void AoTeclarNaNova(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        Criar(sender, e);
        e.Handled = true;
    }

    private void Renomear(object sender, RoutedEventArgs e)
    {
        if (sender is not Button botao || botao.Tag is not Item item)
            return;

        var aviso = _vinculos.RenomearCategoria(item.Categoria.Id, item.Nome);
        Aviso.Text = aviso ?? "";
        if (aviso is not null)
            return;

        FecharExclusao();
        Recarregar();
    }

    /// <summary>
    /// Categoria sem uso sai na hora. Em uso, a tela pergunta o que fazer com os vínculos dela.
    /// </summary>
    private void Excluir(object sender, RoutedEventArgs e)
    {
        if (sender is not Button botao || botao.Tag is not Item item)
            return;

        var plano = _vinculos.ExcluirCategoria(item.Categoria.Id);
        if (plano.Pode)
        {
            Aviso.Text = "";
            FecharExclusao();
            Recarregar();
            return;
        }

        if (plano.EmUso == 0)
        {
            Aviso.Text = plano.Aviso ?? "";
            FecharExclusao();
            Recarregar();
            return;
        }

        Aviso.Text = "";
        _excluindo = item.Categoria;
        TextoDaExclusao.Text = $"{item.Categoria.Nome} está em {plano.Onde}. O que fazer com {(plano.EmUso == 1 ? "ele" : "eles")}?";
        var outras = _vinculos.ListarCategorias().Where(outra => outra.Id != item.Categoria.Id).ToList();
        Substituta.ItemsSource = outras;
        Substituta.SelectedIndex = -1;
        LinhaDaTroca.Visibility = outras.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        OpcaoTrocar.IsChecked = false;
        OpcaoRetirar.IsChecked = outras.Count == 0;
        PainelDaExclusao.Visibility = Visibility.Visible;
    }

    private void AoEscolherSubstituta(object sender, SelectionChangedEventArgs e)
    {
        if (Substituta.SelectedItem is not null)
            OpcaoTrocar.IsChecked = true;
    }

    private void ConfirmarExclusao(object sender, RoutedEventArgs e)
    {
        if (_excluindo is null)
            return;

        EscolhaAoExcluirCategoria escolha;
        if (OpcaoTrocar.IsChecked == true)
        {
            if (Substituta.SelectedItem is not Categoria outra)
            {
                Aviso.Text = "Escolha a categoria que entra no lugar.";
                return;
            }

            escolha = EscolhaAoExcluirCategoria.Trocar(outra.Id);
        }
        else if (OpcaoRetirar.IsChecked == true)
        {
            escolha = EscolhaAoExcluirCategoria.Retirar;
        }
        else
        {
            Aviso.Text = "Escolha trocar por outra categoria ou tirar a categoria desses vínculos.";
            return;
        }

        var plano = _vinculos.ExcluirCategoria(_excluindo.Id, escolha);
        Aviso.Text = plano.Pode ? "" : plano.Aviso ?? "";
        if (!plano.Pode && plano.EmUso > 0)
            return;

        FecharExclusao();
        Recarregar();
    }

    private void DesistirDaExclusao(object sender, RoutedEventArgs e)
    {
        Aviso.Text = "";
        FecharExclusao();
    }

    private void FecharExclusao()
    {
        _excluindo = null;
        PainelDaExclusao.Visibility = Visibility.Collapsed;
    }

    private void Recarregar()
    {
        var uso = _vinculos.ListarVinculos()
            .Where(vinculo => vinculo.CategoriaId is not null)
            .GroupBy(vinculo => vinculo.CategoriaId!.Value)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.Count());
        var itens = _vinculos.ListarCategorias()
            .Select(categoria => new Item(categoria, uso.TryGetValue(categoria.Id, out var quantos) ? quantos : 0))
            .ToList();
        Lista.ItemsSource = itens;
        Vazio.Visibility = itens.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private sealed class Item(Categoria categoria, int emUso)
    {
        public Categoria Categoria { get; } = categoria;

        public string Nome { get; set; } = categoria.Nome;

        public string Uso { get; } = emUso switch
        {
            0 => "sem uso",
            1 => "1 vínculo",
            _ => $"{emUso} vínculos",
        };
    }
}
