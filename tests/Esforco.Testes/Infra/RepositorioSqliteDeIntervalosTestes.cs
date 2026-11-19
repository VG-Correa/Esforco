using Esforco.Dominio;
using Esforco.Infra;
using Microsoft.Data.Sqlite;

namespace Esforco.Testes.Infra;

public sealed class RepositorioSqliteDeIntervalosTestes : IDisposable
{
    private readonly string _pasta = Path.Combine(Path.GetTempPath(), "esforco-testes", Guid.NewGuid().ToString("N"));

    public RepositorioSqliteDeIntervalosTestes() => Directory.CreateDirectory(_pasta);

    private string Banco => Path.Combine(_pasta, "esforco.db");

    [Fact]
    public void Grava_e_le_o_intervalo()
    {
        var inicio = new DateTimeOffset(2026, 11, 17, 9, 15, 0, TimeSpan.FromHours(-3));
        var fim = inicio.AddMinutes(8);

        using (var repositorio = new RepositorioSqliteDeIntervalos(Banco))
        {
            var intervalo = IntervaloDeEsforco.Abrir("Word", "C:\\docs\\relatorio.docx - Word", "relatorio.docx", "Centro", false, inicio);
            repositorio.Adicionar(intervalo);
            intervalo.Fechar(fim);
            repositorio.SalvarFechamento(intervalo);
        }

        using var leitura = new RepositorioSqliteDeIntervalos(Banco);
        var gravado = Assert.Single(leitura.Listar());
        Assert.Equal("Word", gravado.Programa);
        Assert.Equal("relatorio.docx", gravado.Contexto);
        Assert.Equal("Centro", gravado.Escritorio);
        Assert.False(gravado.Minimizada);
        Assert.Equal(inicio, gravado.Inicio);
        Assert.Equal(fim, gravado.Fim);
        Assert.Null(leitura.ObterAberto());
    }

    [Fact]
    public void Intervalo_aberto_interrompido_fecha_no_ultimo_visto()
    {
        var inicio = new DateTimeOffset(2026, 11, 17, 11, 0, 0, TimeSpan.FromHours(-3));
        var visto = inicio.AddMinutes(5);

        using (var repositorio = new RepositorioSqliteDeIntervalos(Banco))
        {
            var intervalo = IntervaloDeEsforco.Abrir("Chrome", "Caixa - Google Chrome", "Caixa", "", true, inicio);
            repositorio.Adicionar(intervalo);
            repositorio.MarcarVisto(intervalo.Id!.Value, visto);
        }

        using var leitura = new RepositorioSqliteDeIntervalos(Banco);
        var gravado = Assert.Single(leitura.Listar());
        Assert.Equal(visto, gravado.Fim);
        Assert.True(gravado.Minimizada);
        Assert.Null(leitura.ObterAberto());
    }

    [Fact]
    public void Lista_em_ordem_cronologica()
    {
        var inicio = new DateTimeOffset(2026, 11, 17, 8, 0, 0, TimeSpan.FromHours(-3));

        using var repositorio = new RepositorioSqliteDeIntervalos(Banco);
        var segundo = IntervaloDeEsforco.Abrir("B", "b", "b", "", false, inicio.AddHours(1));
        var primeiro = IntervaloDeEsforco.Abrir("A", "a", "a", "", false, inicio);
        repositorio.Adicionar(segundo);
        segundo.Fechar(inicio.AddHours(1).AddMinutes(1));
        repositorio.SalvarFechamento(segundo);
        repositorio.Adicionar(primeiro);

        var lista = repositorio.Listar();

        Assert.Equal(["A", "B"], lista.Select(item => item.Programa));
        Assert.Equal("A", repositorio.ObterAberto()!.Programa);
    }

    [Fact]
    public void Banco_antigo_ganha_as_colunas_do_painel()
    {
        using (var conexao = new SqliteConnection($"Data Source={Banco}"))
        {
            conexao.Open();
            using var comando = conexao.CreateCommand();
            comando.CommandText = """
                CREATE TABLE intervalos (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    programa TEXT NOT NULL,
                    titulo TEXT NOT NULL,
                    contexto TEXT NOT NULL,
                    escritorio TEXT NOT NULL,
                    minimizada INTEGER NOT NULL,
                    inicio TEXT NOT NULL,
                    fim TEXT NULL,
                    visto_em TEXT NOT NULL
                );
                INSERT INTO intervalos (programa, titulo, contexto, escritorio, minimizada, inicio, fim, visto_em)
                VALUES ('Word', 'a.docx', 'a.docx', '', 0, '2026-11-17T08:00:00.0000000-03:00', '2026-11-17T08:05:00.0000000-03:00', '2026-11-17T08:05:00.0000000-03:00');
                """;
            comando.ExecuteNonQuery();
        }

        using var repositorio = new RepositorioSqliteDeIntervalos(Banco);
        var gravado = Assert.Single(repositorio.Listar());
        Assert.Equal("Word", gravado.Programa);
        Assert.Equal("", gravado.Plano);
        Assert.Equal(TipoDeItem.Nenhum, gravado.TipoDoItem);
        Assert.False(gravado.EmSegundoPlano);
    }

    [Fact]
    public void Grava_a_hierarquia_do_painel()
    {
        var inicio = new DateTimeOffset(2026, 11, 17, 14, 0, 0, TimeSpan.FromHours(-3));
        var local = InterpretadorDePainelSplitC.Interpretar(
            "https://app.splitc.com.br/planos/17927?officeId=2200&periodId=49797&tabSelector=statement&panelId=42734");
        Assert.NotNull(local);

        using (var repositorio = new RepositorioSqliteDeIntervalos(Banco))
        {
            var intervalo = IntervaloDeEsforco.AbrirPainel("SplitC", local, false, inicio);
            repositorio.Adicionar(intervalo);
            intervalo.Fechar(inicio.AddMinutes(6));
            repositorio.SalvarFechamento(intervalo);
        }

        using var leitura = new RepositorioSqliteDeIntervalos(Banco);
        var gravado = Assert.Single(leitura.Listar());
        Assert.Equal("Painel SplitC", gravado.Programa);
        Assert.Equal("2200", gravado.Escritorio);
        Assert.Equal("17927", gravado.Plano);
        Assert.Equal("49797", gravado.Periodo);
        Assert.Equal("Dashboards", gravado.Sessao);
        Assert.Equal("42734", gravado.Item);
        Assert.Equal(TipoDeItem.Dashboard, gravado.TipoDoItem);
    }

    [Fact]
    public void Guarda_e_apaga_a_preferencia_do_programa()
    {
        var inicio = new DateTimeOffset(2026, 11, 17, 9, 0, 0, TimeSpan.FromHours(-3));
        using (var repositorio = new RepositorioSqliteDeIntervalos(Banco))
        {
            var chrome = IntervaloDeEsforco.Abrir("Google Chrome", "Gmail", "Gmail", "", false, inicio);
            var word = IntervaloDeEsforco.Abrir("Word", "a.docx", "a.docx", "", false, inicio.AddMinutes(1));
            repositorio.Adicionar(chrome);
            repositorio.Adicionar(word);
            repositorio.SalvarPreferencia(new PreferenciaDePrograma("Google Chrome", true, true, true));
        }

        using (var repositorio = new RepositorioSqliteDeIntervalos(Banco))
        {
            var preferencia = Assert.Single(repositorio.ListarPreferencias());
            Assert.Equal("Google Chrome", preferencia.Programa);
            Assert.True(preferencia.Oculto);
            Assert.True(preferencia.Fixado);
            Assert.True(preferencia.Pausado);

            repositorio.ExcluirPrograma("Google Chrome");
            Assert.Empty(repositorio.ListarPreferencias());
            var restante = Assert.Single(repositorio.Listar());
            Assert.Equal("Word", restante.Programa);
        }
    }

    [Fact]
    public void Guarda_nome_variacao_e_plano_do_escritorio()
    {
        using (var repositorio = new RepositorioSqliteDeIntervalos(Banco))
        {
            repositorio.DefinirNomeDeEscritorio("2200", "NTM");
            repositorio.AdicionarVariacao("2200", "NTM Sul");
            repositorio.DefinirNomeDePlano("2200", "17927", "Comissão");
        }

        using var leitura = new RepositorioSqliteDeIntervalos(Banco);
        var cadastro = Assert.Single(leitura.ListarCadastros());
        Assert.Equal("NTM", cadastro.Nome);
        Assert.Equal("NTM Sul", Assert.Single(cadastro.Variacoes));
        Assert.Equal("Comissão", Assert.Single(cadastro.Planos).Nome);

        leitura.RemoverVariacao("2200", "NTM Sul");
        leitura.DefinirNomeDePlano("2200", "17927", "");
        leitura.DefinirNomeDeEscritorio("2200", "");
        Assert.Empty(leitura.ListarCadastros());
    }

    [Fact]
    public void Banco_antigo_guarda_o_retomado()
    {
        using (var conexao = new SqliteConnection($"Data Source={Banco}"))
        {
            conexao.Open();
            using var comando = conexao.CreateCommand();
            comando.CommandText = """
                CREATE TABLE intervalos (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    programa TEXT NOT NULL,
                    titulo TEXT NOT NULL,
                    contexto TEXT NOT NULL,
                    escritorio TEXT NOT NULL,
                    plano TEXT NOT NULL DEFAULT '',
                    periodo TEXT NOT NULL DEFAULT '',
                    sessao TEXT NOT NULL DEFAULT '',
                    item TEXT NOT NULL DEFAULT '',
                    tipo_item INTEGER NOT NULL DEFAULT 0,
                    minimizada INTEGER NOT NULL,
                    segundo_plano INTEGER NOT NULL DEFAULT 0,
                    inicio TEXT NOT NULL,
                    fim TEXT NULL,
                    visto_em TEXT NOT NULL
                );
                """;
            comando.ExecuteNonQuery();
        }

        var inicio = new DateTimeOffset(2026, 11, 17, 9, 0, 0, TimeSpan.FromHours(-3));
        using (var repositorio = new RepositorioSqliteDeIntervalos(Banco))
        {
            var antigo = IntervaloDeEsforco.Abrir("Word", "a.docx - Word", "a.docx", "", false, inicio);
            repositorio.Adicionar(antigo);
            antigo.Fechar(inicio.AddMinutes(1));
            repositorio.SalvarFechamento(antigo);

            var retomado = IntervaloDeEsforco.Abrir(
                "Google Chrome",
                "Meet: vct <> splitc - Google Chrome",
                "Meet: vct <> splitc",
                "",
                false,
                inicio.AddMinutes(2));
            retomado.MarcarRetomado();
            repositorio.Adicionar(retomado);
        }

        using var leitura = new RepositorioSqliteDeIntervalos(Banco);
        var lista = leitura.Listar().OrderBy(item => item.Inicio).ToList();
        Assert.False(lista[0].Retomado);
        Assert.True(lista[1].Retomado);
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
}
