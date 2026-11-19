using Esforco.Aplicacao;
using Esforco.Dominio;
using Esforco.Infra;
using Esforco.Testes.Apoio;
using Microsoft.Data.Sqlite;

namespace Esforco.Testes.Infra;

public sealed class RepositorioSqliteDeVinculosTestes : IDisposable
{
    private static readonly string[] TabelasAntigas =
    [
        "intervalos",
        "preferencias",
        "escritorios",
        "variacoes_escritorio",
        "ocultos_escritorio",
        "nomes_plano",
    ];

    private readonly string _pasta = Path.Combine(Path.GetTempPath(), "esforco-testes", Guid.NewGuid().ToString("N"));

    public RepositorioSqliteDeVinculosTestes() => Directory.CreateDirectory(_pasta);

    private string Banco => Path.Combine(_pasta, "esforco.db");

    [Fact]
    public void Grava_e_le_vinculo_categoria_endereco_e_configuracao()
    {
        var inicio = new DateTimeOffset(2026, 11, 17, 9, 15, 0, TimeSpan.FromHours(-3));
        long id;
        Categoria categoria;
        using (var repositorio = new RepositorioSqliteDeVinculos(Banco))
        {
            categoria = repositorio.AdicionarCategoria("Contratos");
            var gravado = repositorio.AdicionarVinculo(new VinculoDeEscritorio(
                0,
                "2200",
                categoria.Id,
                AlcanceDoVinculo.Aba,
                "Google Chrome",
                "Sala - Google Chrome",
                "app.gather.town/app/sala",
                ValidadeDoVinculo.Inatividade,
                inicio,
                TimeSpan.FromMinutes(45),
                false,
                inicio.AddSeconds(3)));
            id = gravado.Id;
            repositorio.AtualizarVinculo(gravado with { EncerradoEm = inicio.AddHours(2) });
            repositorio.GuardarEndereco(12, "app.gather.town/app/sala");
            repositorio.GuardarEndereco(12, "app.gather.town/app/outra");
            repositorio.GuardarEndereco(13, "teams.microsoft.com/v2");
            repositorio.ManterEnderecos([12]);
            repositorio.GravarConfiguracao("atalho_vinculo", "Ctrl+Alt+E");
            repositorio.GravarConfiguracao("atalho_vinculo", "Ctrl+Shift+F9");
        }

        using var leitura = new RepositorioSqliteDeVinculos(Banco);
        var vinculo = Assert.Single(leitura.ListarVinculos());
        Assert.True(id > 0);
        Assert.Equal(id, vinculo.Id);
        Assert.Equal("2200", vinculo.Escritorio);
        Assert.Equal(categoria.Id, vinculo.CategoriaId);
        Assert.Equal(AlcanceDoVinculo.Aba, vinculo.Alcance);
        Assert.Equal("Google Chrome", vinculo.Programa);
        Assert.Equal("Sala - Google Chrome", vinculo.Titulo);
        Assert.Equal("app.gather.town/app/sala", vinculo.Endereco);
        Assert.Equal(ValidadeDoVinculo.Inatividade, vinculo.Validade);
        Assert.Equal(inicio, vinculo.Inicio);
        Assert.Equal(TimeSpan.FromMinutes(45), vinculo.Duracao);
        Assert.False(vinculo.SoRegistro);
        Assert.Equal(inicio.AddSeconds(3), vinculo.CriadoEm);
        Assert.Equal(inicio.AddHours(2), vinculo.EncerradoEm);
        Assert.Equal(categoria, Assert.Single(leitura.ListarCategorias()));
        var endereco = Assert.Single(leitura.ListarEnderecos());
        Assert.Equal(12, endereco.Key);
        Assert.Equal("app.gather.town/app/outra", endereco.Value);
        Assert.Equal("Ctrl+Shift+F9", leitura.LerConfiguracao("atalho_vinculo"));
        Assert.Null(leitura.LerConfiguracao("outra"));
    }

    [Fact]
    public void Excluir_categoria_em_uso_troca_ou_retira_a_categoria_dos_vinculos()
    {
        using var repositorio = new RepositorioSqliteDeVinculos(Banco);
        var servico = new ServicoDeVinculos(repositorio, new RelogioManual { Instante = Vinculos.Em(17, 9, 0) });
        var contratos = servico.CriarCategoria("Contratos").Categoria!;
        var suporte = servico.CriarCategoria("Suporte").Categoria!;
        var word = servico.Criar(Pedido("Word", contratos.Id)).Vinculo!;
        var excel = servico.Criar(Pedido("Excel", suporte.Id)).Vinculo!;

        var semEscolha = servico.ExcluirCategoria(contratos.Id);

        Assert.False(semEscolha.Pode);
        Assert.Equal(2, repositorio.ListarCategorias().Count);

        var troca = servico.ExcluirCategoria(contratos.Id, EscolhaAoExcluirCategoria.Trocar(suporte.Id));

        Assert.True(troca.Pode);
        Assert.Equal(suporte, Assert.Single(repositorio.ListarCategorias()));
        Assert.All(repositorio.ListarVinculos(), vinculo => Assert.Equal(suporte.Id, vinculo.CategoriaId));

        var retirada = servico.ExcluirCategoria(suporte.Id, EscolhaAoExcluirCategoria.Retirar);

        Assert.True(retirada.Pode);
        Assert.Empty(repositorio.ListarCategorias());
        var vinculos = repositorio.ListarVinculos();
        Assert.Equal([word.Id, excel.Id], vinculos.Select(vinculo => vinculo.Id).ToArray());
        Assert.All(vinculos, vinculo => Assert.Null(vinculo.CategoriaId));
    }

    [Fact]
    public void Tabelas_novas_nao_alteram_as_que_ja_existiam_nem_os_intervalos_gravados()
    {
        var inicio = new DateTimeOffset(2026, 11, 17, 9, 0, 0, TimeSpan.FromHours(-3));
        using var intervalos = new RepositorioSqliteDeIntervalos(Banco);
        var antigo = IntervaloDeEsforco.Abrir("Word", "contrato.docx - Word", "contrato.docx", "", false, inicio);
        intervalos.Adicionar(antigo);
        antigo.Fechar(inicio.AddMinutes(30));
        intervalos.SalvarFechamento(antigo);
        intervalos.DefinirNomeDeEscritorio("2200", "NTM");
        var esquemaAntes = Esquema();
        var linhasAntes = Linhas();

        using var vinculos = new RepositorioSqliteDeVinculos(Banco);
        var servico = new ServicoDeVinculos(vinculos, new RelogioManual { Instante = inicio.AddHours(1) });
        var categoria = servico.CriarCategoria("Contratos").Categoria!;
        servico.Criar(Pedido("Word", categoria.Id));
        servico.GuardarEndereco(antigo.Id!.Value, "https://exemplo.com/contrato?x=1");
        servico.ExcluirCategoria(categoria.Id, EscolhaAoExcluirCategoria.Retirar);

        Assert.Equal(esquemaAntes, Esquema());
        Assert.Equal(linhasAntes, Linhas());
        var gravado = Assert.Single(intervalos.Listar());
        Assert.Equal("", gravado.Escritorio);
        Assert.Equal(inicio, gravado.Inicio);
        Assert.Equal(inicio.AddMinutes(30), gravado.Fim);
        var linha = Assert.Single(RelatorioDeEscritorios.Montar(
            intervalos.Listar(),
            inicio.AddHours(2),
            intervalos.ListarCadastros(),
            null,
            servico.Contexto()));
        Assert.Equal("NTM", linha.Rotulo);
        Assert.Equal(TimeSpan.FromMinutes(30), linha.DuracaoOutros);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try
        {
            Directory.Delete(_pasta, true);
        }
        catch (IOException)
        {
        }
    }

    private static PedidoDeVinculo Pedido(string programa, long? categoria) =>
        new(
            "2200",
            categoria,
            AlcanceDoVinculo.Programa,
            new AlvoDeVinculo(programa, ""),
            ValidadeDoVinculo.Sempre,
            Vinculos.Em(1, 0, 0),
            null);

    private List<string> Esquema()
    {
        using var conexao = Abrir();
        using var comando = conexao.CreateCommand();
        comando.CommandText = "SELECT name, sql FROM sqlite_master WHERE type = 'table' ORDER BY name;";
        using var leitor = comando.ExecuteReader();
        var lista = new List<string>();
        while (leitor.Read())
        {
            if (TabelasAntigas.Contains(leitor.GetString(0)))
                lista.Add(leitor.GetString(0) + ": " + leitor.GetString(1));
        }

        Assert.Equal(TabelasAntigas.Length, lista.Count);
        return lista;
    }

    private List<string> Linhas()
    {
        using var conexao = Abrir();
        var lista = new List<string>();
        foreach (var tabela in TabelasAntigas)
        {
            using var comando = conexao.CreateCommand();
            comando.CommandText = $"SELECT * FROM {tabela} ORDER BY 1;";
            using var leitor = comando.ExecuteReader();
            while (leitor.Read())
            {
                var valores = new object[leitor.FieldCount];
                leitor.GetValues(valores);
                lista.Add(tabela + ": " + string.Join("|", valores));
            }
        }

        return lista;
    }

    private SqliteConnection Abrir()
    {
        var conexao = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = Banco,
            Mode = SqliteOpenMode.ReadOnly,
            Cache = SqliteCacheMode.Shared,
        }.ToString());
        conexao.Open();
        return conexao;
    }
}
