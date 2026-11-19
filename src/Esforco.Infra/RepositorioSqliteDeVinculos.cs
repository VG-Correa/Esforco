using System.Globalization;
using Esforco.Aplicacao;
using Esforco.Dominio;
using Microsoft.Data.Sqlite;

namespace Esforco.Infra;

public sealed class RepositorioSqliteDeVinculos : IRepositorioDeVinculos, IDisposable
{
    private const string Colunas =
        "id, escritorio, categoria_id, alcance, programa, titulo, endereco, validade, inicio, duracao_segundos, so_registro, criado_em, encerrado_em";

    private readonly SqliteConnection _conexao;
    private readonly object _trava = new();

    public RepositorioSqliteDeVinculos(string caminhoDoBanco)
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
        Executar(BancoDoEsforco.TabelasDeVinculos);
    }

    public IReadOnlyList<Categoria> ListarCategorias()
    {
        lock (_trava)
        {
            using var comando = _conexao.CreateCommand();
            comando.CommandText = "SELECT id, nome FROM categorias ORDER BY nome COLLATE NOCASE ASC, id ASC;";
            using var leitor = comando.ExecuteReader();
            var lista = new List<Categoria>();
            while (leitor.Read())
                lista.Add(new Categoria(leitor.GetInt64(0), leitor.GetString(1)));
            return lista;
        }
    }

    public Categoria AdicionarCategoria(string nome)
    {
        lock (_trava)
        {
            using var comando = _conexao.CreateCommand();
            comando.CommandText = "INSERT INTO categorias (nome) VALUES ($nome); SELECT last_insert_rowid();";
            comando.Parameters.AddWithValue("$nome", nome);
            return new Categoria((long)comando.ExecuteScalar()!, nome);
        }
    }

    public void RenomearCategoria(long id, string nome)
    {
        lock (_trava)
        {
            using var comando = _conexao.CreateCommand();
            comando.CommandText = "UPDATE categorias SET nome = $nome WHERE id = $id;";
            comando.Parameters.AddWithValue("$nome", nome);
            comando.Parameters.AddWithValue("$id", id);
            comando.ExecuteNonQuery();
        }
    }

    public void ExcluirCategoria(long id, long? trocarPor)
    {
        lock (_trava)
        {
            using var transacao = _conexao.BeginTransaction();
            using (var troca = _conexao.CreateCommand())
            {
                troca.Transaction = transacao;
                troca.CommandText = "UPDATE vinculos_escritorio SET categoria_id = $nova WHERE categoria_id = $id;";
                troca.Parameters.AddWithValue("$nova", trocarPor is long nova ? nova : DBNull.Value);
                troca.Parameters.AddWithValue("$id", id);
                troca.ExecuteNonQuery();
            }

            using (var exclusao = _conexao.CreateCommand())
            {
                exclusao.Transaction = transacao;
                exclusao.CommandText = "DELETE FROM categorias WHERE id = $id;";
                exclusao.Parameters.AddWithValue("$id", id);
                exclusao.ExecuteNonQuery();
            }

            transacao.Commit();
        }
    }

    public IReadOnlyList<VinculoDeEscritorio> ListarVinculos()
    {
        lock (_trava)
        {
            using var comando = _conexao.CreateCommand();
            comando.CommandText = $"SELECT {Colunas} FROM vinculos_escritorio ORDER BY id ASC;";
            using var leitor = comando.ExecuteReader();
            var lista = new List<VinculoDeEscritorio>();
            while (leitor.Read())
                lista.Add(Ler(leitor));
            return lista;
        }
    }

    public VinculoDeEscritorio AdicionarVinculo(VinculoDeEscritorio vinculo)
    {
        ArgumentNullException.ThrowIfNull(vinculo);
        lock (_trava)
        {
            using var comando = _conexao.CreateCommand();
            comando.CommandText = """
                INSERT INTO vinculos_escritorio (
                    escritorio, categoria_id, alcance, programa, titulo, endereco, validade,
                    inicio, duracao_segundos, so_registro, criado_em, encerrado_em)
                VALUES (
                    $escritorio, $categoria, $alcance, $programa, $titulo, $endereco, $validade,
                    $inicio, $duracao, $registro, $criado, $encerrado);
                SELECT last_insert_rowid();
                """;
            Preencher(comando, vinculo);
            return vinculo with { Id = (long)comando.ExecuteScalar()! };
        }
    }

    public void AtualizarVinculo(VinculoDeEscritorio vinculo)
    {
        ArgumentNullException.ThrowIfNull(vinculo);
        lock (_trava)
        {
            using var comando = _conexao.CreateCommand();
            comando.CommandText = """
                UPDATE vinculos_escritorio
                SET escritorio = $escritorio, categoria_id = $categoria, alcance = $alcance, programa = $programa,
                    titulo = $titulo, endereco = $endereco, validade = $validade, inicio = $inicio,
                    duracao_segundos = $duracao, so_registro = $registro, criado_em = $criado, encerrado_em = $encerrado
                WHERE id = $id;
                """;
            Preencher(comando, vinculo);
            comando.Parameters.AddWithValue("$id", vinculo.Id);
            comando.ExecuteNonQuery();
        }
    }

    public void ExcluirVinculo(long id)
    {
        lock (_trava)
        {
            using var comando = _conexao.CreateCommand();
            comando.CommandText = "DELETE FROM vinculos_escritorio WHERE id = $id;";
            comando.Parameters.AddWithValue("$id", id);
            comando.ExecuteNonQuery();
        }
    }

    public IReadOnlyDictionary<long, string> ListarEnderecos()
    {
        lock (_trava)
        {
            using var comando = _conexao.CreateCommand();
            comando.CommandText = "SELECT intervalo_id, endereco FROM enderecos_intervalo;";
            using var leitor = comando.ExecuteReader();
            var mapa = new Dictionary<long, string>();
            while (leitor.Read())
                mapa[leitor.GetInt64(0)] = leitor.GetString(1);
            return mapa;
        }
    }

    public void GuardarEndereco(long intervalo, string endereco)
    {
        lock (_trava)
        {
            using var comando = _conexao.CreateCommand();
            comando.CommandText = """
                INSERT INTO enderecos_intervalo (intervalo_id, endereco) VALUES ($id, $endereco)
                ON CONFLICT(intervalo_id) DO UPDATE SET endereco = excluded.endereco;
                """;
            comando.Parameters.AddWithValue("$id", intervalo);
            comando.Parameters.AddWithValue("$endereco", endereco);
            comando.ExecuteNonQuery();
        }
    }

    public void ManterEnderecos(IReadOnlyCollection<long> intervalos)
    {
        ArgumentNullException.ThrowIfNull(intervalos);
        var vivos = intervalos as ISet<long> ?? new HashSet<long>(intervalos);
        lock (_trava)
        {
            var sobrando = new List<long>();
            using (var consulta = _conexao.CreateCommand())
            {
                consulta.CommandText = "SELECT intervalo_id FROM enderecos_intervalo;";
                using var leitor = consulta.ExecuteReader();
                while (leitor.Read())
                {
                    var id = leitor.GetInt64(0);
                    if (!vivos.Contains(id))
                        sobrando.Add(id);
                }
            }

            foreach (var id in sobrando)
            {
                using var comando = _conexao.CreateCommand();
                comando.CommandText = "DELETE FROM enderecos_intervalo WHERE intervalo_id = $id;";
                comando.Parameters.AddWithValue("$id", id);
                comando.ExecuteNonQuery();
            }
        }
    }

    public string? LerConfiguracao(string chave)
    {
        lock (_trava)
        {
            using var comando = _conexao.CreateCommand();
            comando.CommandText = "SELECT valor FROM configuracoes WHERE chave = $chave;";
            comando.Parameters.AddWithValue("$chave", chave);
            return comando.ExecuteScalar() as string;
        }
    }

    public void GravarConfiguracao(string chave, string valor)
    {
        lock (_trava)
        {
            using var comando = _conexao.CreateCommand();
            comando.CommandText = """
                INSERT INTO configuracoes (chave, valor) VALUES ($chave, $valor)
                ON CONFLICT(chave) DO UPDATE SET valor = excluded.valor;
                """;
            comando.Parameters.AddWithValue("$chave", chave);
            comando.Parameters.AddWithValue("$valor", valor);
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

    private static void Preencher(SqliteCommand comando, VinculoDeEscritorio vinculo)
    {
        comando.Parameters.AddWithValue("$escritorio", vinculo.Escritorio);
        comando.Parameters.AddWithValue("$categoria", vinculo.CategoriaId is long categoria ? categoria : DBNull.Value);
        comando.Parameters.AddWithValue("$alcance", (int)vinculo.Alcance);
        comando.Parameters.AddWithValue("$programa", vinculo.Programa);
        comando.Parameters.AddWithValue("$titulo", vinculo.Titulo);
        comando.Parameters.AddWithValue("$endereco", vinculo.Endereco);
        comando.Parameters.AddWithValue("$validade", (int)vinculo.Validade);
        comando.Parameters.AddWithValue("$inicio", Iso(vinculo.Inicio));
        comando.Parameters.AddWithValue("$duracao", vinculo.Duracao is TimeSpan duracao ? duracao.TotalSeconds : DBNull.Value);
        comando.Parameters.AddWithValue("$registro", vinculo.SoRegistro ? 1 : 0);
        comando.Parameters.AddWithValue("$criado", Iso(vinculo.CriadoEm));
        comando.Parameters.AddWithValue("$encerrado", vinculo.EncerradoEm is DateTimeOffset fim ? Iso(fim) : DBNull.Value);
    }

    private static VinculoDeEscritorio Ler(SqliteDataReader leitor)
    {
        var alcance = (AlcanceDoVinculo)leitor.GetInt64(3);
        if (!Enum.IsDefined(alcance))
            alcance = AlcanceDoVinculo.Programa;

        var validade = (ValidadeDoVinculo)leitor.GetInt64(7);
        if (!Enum.IsDefined(validade))
            validade = ValidadeDoVinculo.Sempre;

        return new VinculoDeEscritorio(
            leitor.GetInt64(0),
            leitor.GetString(1),
            leitor.IsDBNull(2) ? null : leitor.GetInt64(2),
            alcance,
            leitor.GetString(4),
            leitor.GetString(5),
            leitor.GetString(6),
            validade,
            IsoDe(leitor.GetString(8)),
            leitor.IsDBNull(9) ? null : TimeSpan.FromSeconds(leitor.GetDouble(9)),
            leitor.GetInt64(10) != 0,
            IsoDe(leitor.GetString(11)),
            leitor.IsDBNull(12) ? null : IsoDe(leitor.GetString(12)));
    }

    private static string Iso(DateTimeOffset valor) =>
        valor.ToString("O", CultureInfo.InvariantCulture);

    private static DateTimeOffset IsoDe(string texto) =>
        DateTimeOffset.Parse(texto, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
}
