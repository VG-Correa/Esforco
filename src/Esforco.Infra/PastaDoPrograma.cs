namespace Esforco.Infra;

public static class PastaDoPrograma
{
    public static string Obter()
    {
        var caminho = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(caminho))
        {
            var pasta = Path.GetDirectoryName(caminho);
            if (!string.IsNullOrWhiteSpace(pasta))
                return pasta;
        }

        return AppContext.BaseDirectory;
    }
}
