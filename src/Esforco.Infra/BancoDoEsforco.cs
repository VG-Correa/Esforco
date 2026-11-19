namespace Esforco.Infra;

public static class BancoDoEsforco
{
    public const string Pasta = "db";
    public const string MarcadorDaRaiz = "Esforco.slnx";

    /// <summary>
    /// Tabelas do vínculo manual. São só criadas, se faltarem: nenhuma tabela que já existia é alterada
    /// e nenhum intervalo gravado é mexido. enderecos_intervalo guarda o endereço da aba apenas dos
    /// intervalos que casam com um vínculo por endereço. contagens_janela guarda como cada janela ou
    /// programa conta o tempo: só em foco ou enquanto estiver aberta. registros_manuais guarda o tempo
    /// lançado à mão, que não vem da leitura de nenhuma janela.
    /// </summary>
    public const string TabelasDeVinculos = """
        CREATE TABLE IF NOT EXISTS categorias (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            nome TEXT NOT NULL
        );
        CREATE TABLE IF NOT EXISTS vinculos_escritorio (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            escritorio TEXT NOT NULL,
            categoria_id INTEGER NULL,
            alcance INTEGER NOT NULL,
            programa TEXT NOT NULL,
            titulo TEXT NOT NULL,
            endereco TEXT NOT NULL,
            validade INTEGER NOT NULL,
            inicio TEXT NOT NULL,
            duracao_segundos REAL NULL,
            so_registro INTEGER NOT NULL,
            criado_em TEXT NOT NULL,
            encerrado_em TEXT NULL
        );
        CREATE TABLE IF NOT EXISTS enderecos_intervalo (
            intervalo_id INTEGER PRIMARY KEY,
            endereco TEXT NOT NULL
        );
        CREATE TABLE IF NOT EXISTS configuracoes (
            chave TEXT PRIMARY KEY,
            valor TEXT NOT NULL
        );
        CREATE TABLE IF NOT EXISTS contagens_janela (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            alcance INTEGER NOT NULL,
            programa TEXT NOT NULL,
            titulo TEXT NOT NULL,
            modo INTEGER NOT NULL,
            salva_em TEXT NOT NULL
        );
        CREATE TABLE IF NOT EXISTS registros_manuais (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            escritorio TEXT NOT NULL,
            categoria_id INTEGER NULL,
            descricao TEXT NOT NULL,
            inicio TEXT NOT NULL,
            fim TEXT NOT NULL,
            criado_em TEXT NOT NULL,
            alterado_em TEXT NOT NULL
        );
        """;

    public static string Resolver(string pastaDoExecutavel, string? diretorioAtual = null, string? raizInformada = null)
    {
        var raiz = RaizValida(raizInformada)
            ?? ProcurarRaiz(pastaDoExecutavel)
            ?? ProcurarRaiz(diretorioAtual);
        if (raiz is null)
            return Path.Combine(pastaDoExecutavel, RepositorioSqliteDeIntervalos.NomeDoArquivo);

        var destino = Path.Combine(raiz, Pasta, RepositorioSqliteDeIntervalos.NomeDoArquivo);
        if (!File.Exists(destino))
        {
            CopiarSeFaltar(destino, Path.Combine(pastaDoExecutavel, RepositorioSqliteDeIntervalos.NomeDoArquivo));
            CopiarSeFaltar(destino, Path.Combine(raiz, "saida", RepositorioSqliteDeIntervalos.NomeDoArquivo));
        }

        return destino;
    }

    private static string? RaizValida(string? caminho)
    {
        if (string.IsNullOrWhiteSpace(caminho))
            return null;

        var cheio = Path.GetFullPath(caminho);
        return File.Exists(Path.Combine(cheio, MarcadorDaRaiz)) ? cheio : null;
    }

    private static string? ProcurarRaiz(string? inicio)
    {
        if (string.IsNullOrWhiteSpace(inicio))
            return null;

        DirectoryInfo? pasta;
        try
        {
            var cheio = Path.GetFullPath(inicio);
            pasta = File.Exists(cheio) ? new DirectoryInfo(Path.GetDirectoryName(cheio)!) : new DirectoryInfo(cheio);
        }
        catch (Exception)
        {
            return null;
        }

        while (pasta is not null)
        {
            if (File.Exists(Path.Combine(pasta.FullName, MarcadorDaRaiz)))
                return pasta.FullName;
            pasta = pasta.Parent;
        }

        return null;
    }

    private static void CopiarSeFaltar(string destino, string origem)
    {
        if (File.Exists(destino) || !File.Exists(origem))
            return;

        if (MesmoArquivo(destino, origem))
            return;

        var pasta = Path.GetDirectoryName(destino);
        if (!string.IsNullOrWhiteSpace(pasta))
            Directory.CreateDirectory(pasta);

        Copiar(origem, destino);
        Copiar(origem + "-wal", destino + "-wal");
        Copiar(origem + "-shm", destino + "-shm");
    }

    private static void Copiar(string origem, string destino)
    {
        if (!File.Exists(origem) || File.Exists(destino))
            return;

        try
        {
            using var leitura = new FileStream(origem, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var escrita = new FileStream(destino, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            leitura.CopyTo(escrita);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            if (File.Exists(destino))
                File.Delete(destino);
        }
    }

    private static bool MesmoArquivo(string esquerda, string direita)
    {
        try
        {
            return string.Equals(Path.GetFullPath(esquerda), Path.GetFullPath(direita), StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception)
        {
            return false;
        }
    }
}
