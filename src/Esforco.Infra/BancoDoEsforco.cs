namespace Esforco.Infra;

public static class BancoDoEsforco
{
    public const string Pasta = "db";
    public const string MarcadorDaRaiz = "Esforco.slnx";

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
