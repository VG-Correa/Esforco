using System.Globalization;
using Esforco.Aplicacao;
using Esforco.Dominio;
using Microsoft.Data.Sqlite;

namespace Esforco.Infra;

public sealed class RepositorioSqliteDeIntervalos : IRepositorioDeIntervalos, IDisposable
{
    public const string NomeDoArquivo = "esforco.db";

    private readonly SqliteConnection _conexao;
    private readonly object _trava = new();

    public RepositorioSqliteDeIntervalos(string caminhoDoBanco)
    {
        var pasta = Path.GetDirectoryName(caminhoDoBanco);
        if (!string.IsNullOrWhiteSpace(pasta))
            Directory.CreateDirectory(pasta);

        var texto = new SqliteConnectionStringBuilder
        {
            DataSource = caminhoDoBanco,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
        }.ToString();

        _conexao = new SqliteConnection(texto);
        _conexao.Open();
        Executar("PRAGMA busy_timeout=2000;");
        Executar("""
            CREATE TABLE IF NOT EXISTS intervalos (
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
                retomado INTEGER NOT NULL DEFAULT 0,
                inicio TEXT NOT NULL,
                fim TEXT NULL,
                visto_em TEXT NOT NULL
            );
            """);
        GarantirColuna("plano", "TEXT NOT NULL DEFAULT ''");
        GarantirColuna("periodo", "TEXT NOT NULL DEFAULT ''");
        GarantirColuna("sessao", "TEXT NOT NULL DEFAULT ''");
        GarantirColuna("item", "TEXT NOT NULL DEFAULT ''");
        GarantirColuna("tipo_item", "INTEGER NOT NULL DEFAULT 0");
        GarantirColuna("segundo_plano", "INTEGER NOT NULL DEFAULT 0");
        GarantirColuna("retomado", "INTEGER NOT NULL DEFAULT 0");
        Executar("""
            CREATE TABLE IF NOT EXISTS preferencias (
                programa TEXT PRIMARY KEY,
                oculto INTEGER NOT NULL,
                fixado INTEGER NOT NULL,
                pausado INTEGER NOT NULL
            );
            """);
        Executar("""
            CREATE TABLE IF NOT EXISTS escritorios (
                id TEXT PRIMARY KEY,
                nome TEXT NOT NULL
            );
            """);
        Executar("""
            CREATE TABLE IF NOT EXISTS variacoes_escritorio (
                escritorio TEXT NOT NULL,
                variacao TEXT NOT NULL,
                PRIMARY KEY (escritorio, variacao)
            );
            """);
        Executar("""
            CREATE TABLE IF NOT EXISTS ocultos_escritorio (
                por_id INTEGER NOT NULL,
                texto TEXT NOT NULL,
                PRIMARY KEY (por_id, texto)
            );
            """);
        Executar("""
            CREATE TABLE IF NOT EXISTS nomes_plano (
                escritorio TEXT NOT NULL,
                plano TEXT NOT NULL,
                nome TEXT NOT NULL,
                PRIMARY KEY (escritorio, plano)
            );
            """);
        Executar("""
            UPDATE intervalos
            SET fim = COALESCE(visto_em, inicio)
            WHERE fim IS NULL;
            """);
    }

    public static string CaminhoAoLadoDoExecutavel() =>
        Path.Combine(PastaDoPrograma.Obter(), NomeDoArquivo);

    public IntervaloDeEsforco? ObterAberto()
    {
        lock (_trava)
        {
            using var comando = _conexao.CreateCommand();
            comando.CommandText = """
                SELECT id, programa, titulo, contexto, escritorio, plano, periodo, sessao, item, tipo_item, minimizada, segundo_plano, retomado, inicio, fim
                FROM intervalos
                WHERE fim IS NULL AND segundo_plano = 0
                ORDER BY id DESC
                LIMIT 1;
                """;
            using var leitor = comando.ExecuteReader();
            return leitor.Read() ? Ler(leitor) : null;
        }
    }

    public IntervaloDeEsforco? ObterReuniaoEmSegundoPlano()
    {
        lock (_trava)
        {
            using var comando = _conexao.CreateCommand();
            comando.CommandText = """
                SELECT id, programa, titulo, contexto, escritorio, plano, periodo, sessao, item, tipo_item, minimizada, segundo_plano, retomado, inicio, fim
                FROM intervalos
                WHERE fim IS NULL AND segundo_plano = 1
                ORDER BY id DESC
                LIMIT 1;
                """;
            using var leitor = comando.ExecuteReader();
            return leitor.Read() ? Ler(leitor) : null;
        }
    }

    public IReadOnlyList<IntervaloDeEsforco> ListarEmSegundoPlano()
    {
        lock (_trava)
        {
            using var comando = _conexao.CreateCommand();
            comando.CommandText = """
                SELECT id, programa, titulo, contexto, escritorio, plano, periodo, sessao, item, tipo_item, minimizada, segundo_plano, retomado, inicio, fim
                FROM intervalos
                WHERE fim IS NULL AND segundo_plano = 1
                ORDER BY id ASC;
                """;
            using var leitor = comando.ExecuteReader();
            var lista = new List<IntervaloDeEsforco>();
            while (leitor.Read())
                lista.Add(Ler(leitor));
            return lista;
        }
    }

    public void DefinirSegundoPlano(IntervaloDeEsforco intervalo, bool emSegundoPlano)
    {
        ArgumentNullException.ThrowIfNull(intervalo);
        if (intervalo.Id is not long id)
            throw new InvalidOperationException("O intervalo precisa estar gravado.");

        intervalo.DefinirSegundoPlano(emSegundoPlano);
        lock (_trava)
        {
            using var comando = _conexao.CreateCommand();
            comando.CommandText = """
                UPDATE intervalos
                SET segundo_plano = $segundo
                WHERE id = $id;
                """;
            comando.Parameters.AddWithValue("$segundo", emSegundoPlano ? 1 : 0);
            comando.Parameters.AddWithValue("$id", id);
            comando.ExecuteNonQuery();
        }
    }

    public void Adicionar(IntervaloDeEsforco intervalo)
    {
        ArgumentNullException.ThrowIfNull(intervalo);
        lock (_trava)
        {
            using var comando = _conexao.CreateCommand();
            comando.CommandText = """
                INSERT INTO intervalos (
                    programa, titulo, contexto, escritorio, plano, periodo, sessao, item, tipo_item,
                    minimizada, segundo_plano, retomado, inicio, fim, visto_em)
                VALUES (
                    $programa, $titulo, $contexto, $escritorio, $plano, $periodo, $sessao, $item, $tipo,
                    $minimizada, $segundo, $retomado, $inicio, NULL, $inicio);
                """;
            Preencher(comando, intervalo);
            comando.Parameters.AddWithValue("$retomado", intervalo.Retomado ? 1 : 0);
            comando.ExecuteNonQuery();
            comando.Parameters.Clear();
            comando.CommandText = "SELECT last_insert_rowid();";
            var id = (long)comando.ExecuteScalar()!;
            intervalo.DefinirId(id);
        }
    }

    public void SalvarFechamento(IntervaloDeEsforco intervalo)
    {
        ArgumentNullException.ThrowIfNull(intervalo);
        if (intervalo.Id is not long id || intervalo.Fim is not DateTimeOffset fim)
            throw new InvalidOperationException("O intervalo precisa estar fechado e gravado.");

        lock (_trava)
        {
            using var comando = _conexao.CreateCommand();
            comando.CommandText = """
                UPDATE intervalos
                SET fim = $fim, visto_em = $fim
                WHERE id = $id;
                """;
            comando.Parameters.AddWithValue("$fim", Iso(fim));
            comando.Parameters.AddWithValue("$id", id);
            comando.ExecuteNonQuery();
        }
    }

    public void MarcarVisto(long id, DateTimeOffset instante)
    {
        lock (_trava)
        {
            using var comando = _conexao.CreateCommand();
            comando.CommandText = """
                UPDATE intervalos
                SET visto_em = $visto
                WHERE id = $id AND fim IS NULL;
                """;
            comando.Parameters.AddWithValue("$visto", Iso(instante));
            comando.Parameters.AddWithValue("$id", id);
            comando.ExecuteNonQuery();
        }
    }

    public IReadOnlyList<IntervaloDeEsforco> Listar()
    {
        lock (_trava)
        {
            using var comando = _conexao.CreateCommand();
            comando.CommandText = """
                SELECT id, programa, titulo, contexto, escritorio, plano, periodo, sessao, item, tipo_item, minimizada, segundo_plano, retomado, inicio, fim
                FROM intervalos
                ORDER BY inicio ASC, id ASC;
                """;
            using var leitor = comando.ExecuteReader();
            var lista = new List<IntervaloDeEsforco>();
            while (leitor.Read())
                lista.Add(Ler(leitor));
            return lista;
        }
    }

    public IReadOnlyList<PreferenciaDePrograma> ListarPreferencias()
    {
        lock (_trava)
        {
            using var comando = _conexao.CreateCommand();
            comando.CommandText = """
                SELECT programa, oculto, fixado, pausado
                FROM preferencias
                ORDER BY programa;
                """;
            using var leitor = comando.ExecuteReader();
            var lista = new List<PreferenciaDePrograma>();
            while (leitor.Read())
            {
                lista.Add(new PreferenciaDePrograma(
                    leitor.GetString(0),
                    leitor.GetInt64(1) != 0,
                    leitor.GetInt64(2) != 0,
                    leitor.GetInt64(3) != 0));
            }

            return lista;
        }
    }

    public void SalvarPreferencia(PreferenciaDePrograma preferencia)
    {
        ArgumentNullException.ThrowIfNull(preferencia);
        lock (_trava)
        {
            using var comando = _conexao.CreateCommand();
            if (!preferencia.TemAlguma)
            {
                comando.CommandText = "DELETE FROM preferencias WHERE programa = $programa;";
                comando.Parameters.AddWithValue("$programa", preferencia.Programa);
                comando.ExecuteNonQuery();
                return;
            }

            comando.CommandText = """
                INSERT INTO preferencias (programa, oculto, fixado, pausado)
                VALUES ($programa, $oculto, $fixado, $pausado)
                ON CONFLICT(programa) DO UPDATE SET
                    oculto = excluded.oculto,
                    fixado = excluded.fixado,
                    pausado = excluded.pausado;
                """;
            comando.Parameters.AddWithValue("$programa", preferencia.Programa);
            comando.Parameters.AddWithValue("$oculto", preferencia.Oculto ? 1 : 0);
            comando.Parameters.AddWithValue("$fixado", preferencia.Fixado ? 1 : 0);
            comando.Parameters.AddWithValue("$pausado", preferencia.Pausado ? 1 : 0);
            comando.ExecuteNonQuery();
        }
    }

    public void ExcluirPrograma(string programa)
    {
        lock (_trava)
        {
            using var transacao = _conexao.BeginTransaction();
            using var comando = _conexao.CreateCommand();
            comando.Transaction = transacao;
            comando.Parameters.AddWithValue("$programa", programa);
            comando.CommandText = "DELETE FROM intervalos WHERE programa = $programa;";
            comando.ExecuteNonQuery();
            comando.CommandText = "DELETE FROM preferencias WHERE programa = $programa;";
            comando.ExecuteNonQuery();
            transacao.Commit();
        }
    }

    public void ExcluirIntervalo(long id)
    {
        lock (_trava)
        {
            using var comando = _conexao.CreateCommand();
            comando.CommandText = "DELETE FROM intervalos WHERE id = $id;";
            comando.Parameters.AddWithValue("$id", id);
            comando.ExecuteNonQuery();
        }
    }

    public void AtualizarLimites(IntervaloDeEsforco intervalo)
    {
        ArgumentNullException.ThrowIfNull(intervalo);
        if (intervalo.Id is not long id)
            throw new InvalidOperationException("O intervalo precisa estar gravado.");

        lock (_trava)
        {
            using var comando = _conexao.CreateCommand();
            comando.CommandText = """
                UPDATE intervalos
                SET inicio = $inicio, fim = $fim, visto_em = COALESCE($fim, $inicio)
                WHERE id = $id;
                """;
            comando.Parameters.AddWithValue("$inicio", Iso(intervalo.Inicio));
            comando.Parameters.AddWithValue("$fim", intervalo.Fim is DateTimeOffset fim ? Iso(fim) : DBNull.Value);
            comando.Parameters.AddWithValue("$id", id);
            comando.ExecuteNonQuery();
        }
    }

    public IReadOnlyList<CadastroDeEscritorio> ListarCadastros()
    {
        lock (_trava)
        {
            var nomes = new Dictionary<string, string>(StringComparer.Ordinal);
            var variacoes = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            var planos = new Dictionary<string, List<NomeDePlano>>(StringComparer.Ordinal);

            using (var comando = _conexao.CreateCommand())
            {
                comando.CommandText = "SELECT id, nome FROM escritorios;";
                using var leitor = comando.ExecuteReader();
                while (leitor.Read())
                    nomes[leitor.GetString(0)] = leitor.GetString(1);
            }

            using (var comando = _conexao.CreateCommand())
            {
                comando.CommandText = "SELECT escritorio, variacao FROM variacoes_escritorio ORDER BY variacao;";
                using var leitor = comando.ExecuteReader();
                while (leitor.Read())
                {
                    var id = leitor.GetString(0);
                    if (!variacoes.TryGetValue(id, out var lista))
                    {
                        lista = [];
                        variacoes[id] = lista;
                    }

                    lista.Add(leitor.GetString(1));
                }
            }

            using (var comando = _conexao.CreateCommand())
            {
                comando.CommandText = "SELECT escritorio, plano, nome FROM nomes_plano ORDER BY plano;";
                using var leitor = comando.ExecuteReader();
                while (leitor.Read())
                {
                    var id = leitor.GetString(0);
                    if (!planos.TryGetValue(id, out var lista))
                    {
                        lista = [];
                        planos[id] = lista;
                    }

                    lista.Add(new NomeDePlano(leitor.GetString(1), leitor.GetString(2)));
                }
            }

            var ids = new HashSet<string>(nomes.Keys, StringComparer.Ordinal);
            ids.UnionWith(variacoes.Keys);
            ids.UnionWith(planos.Keys);
            return ids
                .OrderBy(id => id, StringComparer.Ordinal)
                .Select(id => new CadastroDeEscritorio(
                    id,
                    nomes.GetValueOrDefault(id) ?? "",
                    variacoes.GetValueOrDefault(id) ?? [],
                    planos.GetValueOrDefault(id) ?? []))
                .ToList();
        }
    }

    public IReadOnlyList<OcultacaoDeEscritorio> ListarOcultacoes()
    {
        lock (_trava)
        {
            using var comando = _conexao.CreateCommand();
            comando.CommandText = """
                SELECT por_id, texto
                FROM ocultos_escritorio
                ORDER BY por_id DESC, texto;
                """;
            using var leitor = comando.ExecuteReader();
            var lista = new List<OcultacaoDeEscritorio>();
            while (leitor.Read())
                lista.Add(new OcultacaoDeEscritorio(leitor.GetInt64(0) != 0, leitor.GetString(1)));
            return lista;
        }
    }

    public void AdicionarOcultacao(OcultacaoDeEscritorio ocultacao)
    {
        ArgumentNullException.ThrowIfNull(ocultacao);
        lock (_trava)
        {
            using var comando = _conexao.CreateCommand();
            comando.CommandText = """
                INSERT INTO ocultos_escritorio (por_id, texto)
                VALUES ($porId, $texto)
                ON CONFLICT(por_id, texto) DO NOTHING;
                """;
            comando.Parameters.AddWithValue("$porId", ocultacao.PorId ? 1 : 0);
            comando.Parameters.AddWithValue("$texto", ocultacao.Texto);
            comando.ExecuteNonQuery();
        }
    }

    public void RemoverOcultacao(OcultacaoDeEscritorio ocultacao)
    {
        ArgumentNullException.ThrowIfNull(ocultacao);
        lock (_trava)
        {
            using var comando = _conexao.CreateCommand();
            comando.CommandText = """
                DELETE FROM ocultos_escritorio
                WHERE por_id = $porId AND texto = $texto;
                """;
            comando.Parameters.AddWithValue("$porId", ocultacao.PorId ? 1 : 0);
            comando.Parameters.AddWithValue("$texto", ocultacao.Texto);
            comando.ExecuteNonQuery();
        }
    }

    public void DefinirNomeDeEscritorio(string id, string nome)
    {
        lock (_trava)
        {
            using var comando = _conexao.CreateCommand();
            if (nome.Length == 0)
            {
                comando.CommandText = "DELETE FROM escritorios WHERE id = $id;";
                comando.Parameters.AddWithValue("$id", id);
                comando.ExecuteNonQuery();
                return;
            }

            comando.CommandText = """
                INSERT INTO escritorios (id, nome)
                VALUES ($id, $nome)
                ON CONFLICT(id) DO UPDATE SET nome = excluded.nome;
                """;
            comando.Parameters.AddWithValue("$id", id);
            comando.Parameters.AddWithValue("$nome", nome);
            comando.ExecuteNonQuery();
        }
    }

    public void DefinirNomeDePlano(string escritorio, string plano, string nome)
    {
        lock (_trava)
        {
            using var comando = _conexao.CreateCommand();
            if (nome.Length == 0)
            {
                comando.CommandText = "DELETE FROM nomes_plano WHERE escritorio = $escritorio AND plano = $plano;";
                comando.Parameters.AddWithValue("$escritorio", escritorio);
                comando.Parameters.AddWithValue("$plano", plano);
                comando.ExecuteNonQuery();
                return;
            }

            comando.CommandText = """
                INSERT INTO nomes_plano (escritorio, plano, nome)
                VALUES ($escritorio, $plano, $nome)
                ON CONFLICT(escritorio, plano) DO UPDATE SET nome = excluded.nome;
                """;
            comando.Parameters.AddWithValue("$escritorio", escritorio);
            comando.Parameters.AddWithValue("$plano", plano);
            comando.Parameters.AddWithValue("$nome", nome);
            comando.ExecuteNonQuery();
        }
    }

    public void AdicionarVariacao(string escritorio, string variacao)
    {
        lock (_trava)
        {
            using var comando = _conexao.CreateCommand();
            comando.CommandText = """
                INSERT INTO variacoes_escritorio (escritorio, variacao)
                VALUES ($escritorio, $variacao)
                ON CONFLICT(escritorio, variacao) DO NOTHING;
                """;
            comando.Parameters.AddWithValue("$escritorio", escritorio);
            comando.Parameters.AddWithValue("$variacao", variacao);
            comando.ExecuteNonQuery();
        }
    }

    public void RemoverVariacao(string escritorio, string variacao)
    {
        lock (_trava)
        {
            using var comando = _conexao.CreateCommand();
            comando.CommandText = """
                DELETE FROM variacoes_escritorio
                WHERE escritorio = $escritorio AND variacao = $variacao;
                """;
            comando.Parameters.AddWithValue("$escritorio", escritorio);
            comando.Parameters.AddWithValue("$variacao", variacao);
            comando.ExecuteNonQuery();
        }
    }

    public void Dispose() => _conexao.Dispose();

    private void Executar(string sql)
    {
        using var comando = _conexao.CreateCommand();
        comando.CommandText = sql;
        comando.ExecuteNonQuery();
    }

    private static void Preencher(SqliteCommand comando, IntervaloDeEsforco intervalo)
    {
        comando.Parameters.AddWithValue("$programa", intervalo.Programa);
        comando.Parameters.AddWithValue("$titulo", intervalo.Titulo);
        comando.Parameters.AddWithValue("$contexto", intervalo.Contexto);
        comando.Parameters.AddWithValue("$escritorio", intervalo.Escritorio);
        comando.Parameters.AddWithValue("$plano", intervalo.Plano);
        comando.Parameters.AddWithValue("$periodo", intervalo.Periodo);
        comando.Parameters.AddWithValue("$sessao", intervalo.Sessao);
        comando.Parameters.AddWithValue("$item", intervalo.Item);
        comando.Parameters.AddWithValue("$tipo", (int)intervalo.TipoDoItem);
        comando.Parameters.AddWithValue("$minimizada", intervalo.Minimizada ? 1 : 0);
        comando.Parameters.AddWithValue("$segundo", intervalo.EmSegundoPlano ? 1 : 0);
        comando.Parameters.AddWithValue("$inicio", Iso(intervalo.Inicio));
    }

    private void GarantirColuna(string nome, string definicao)
    {
        var existentes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var consulta = _conexao.CreateCommand())
        {
            consulta.CommandText = "PRAGMA table_info(intervalos);";
            using var leitor = consulta.ExecuteReader();
            while (leitor.Read())
                existentes.Add(leitor.GetString(1));
        }

        if (existentes.Contains(nome))
            return;

        Executar($"ALTER TABLE intervalos ADD COLUMN {nome} {definicao};");
    }

    private static IntervaloDeEsforco Ler(SqliteDataReader leitor)
    {
        var tipo = (TipoDeItem)leitor.GetInt64(leitor.GetOrdinal("tipo_item"));
        if (!Enum.IsDefined(tipo))
            tipo = TipoDeItem.Nenhum;

        var fimOrdinal = leitor.GetOrdinal("fim");
        var fim = leitor.IsDBNull(fimOrdinal) ? (DateTimeOffset?)null : IsoDe(leitor.GetString(fimOrdinal));
        return IntervaloDeEsforco.Restaurar(
            leitor.GetInt64(leitor.GetOrdinal("id")),
            leitor.GetString(leitor.GetOrdinal("programa")),
            leitor.GetString(leitor.GetOrdinal("titulo")),
            leitor.GetString(leitor.GetOrdinal("contexto")),
            leitor.GetString(leitor.GetOrdinal("escritorio")),
            leitor.GetString(leitor.GetOrdinal("plano")),
            leitor.GetString(leitor.GetOrdinal("periodo")),
            leitor.GetString(leitor.GetOrdinal("sessao")),
            leitor.GetString(leitor.GetOrdinal("item")),
            tipo,
            leitor.GetInt64(leitor.GetOrdinal("minimizada")) != 0,
            IsoDe(leitor.GetString(leitor.GetOrdinal("inicio"))),
            fim,
            leitor.GetInt64(leitor.GetOrdinal("segundo_plano")) != 0,
            leitor.GetInt64(leitor.GetOrdinal("retomado")) != 0);
    }

    private static string Iso(DateTimeOffset valor) =>
        valor.ToString("O", CultureInfo.InvariantCulture);

    private static DateTimeOffset IsoDe(string texto) =>
        DateTimeOffset.Parse(texto, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
}
