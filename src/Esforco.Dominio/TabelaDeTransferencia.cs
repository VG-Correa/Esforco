using System.Text;

namespace Esforco.Dominio;

public static class TabelaDeTransferencia
{
    public static string Montar(IEnumerable<(string Codigo, string Percentual, string Nome)> linhas)
    {
        ArgumentNullException.ThrowIfNull(linhas);
        var texto = new StringBuilder();
        foreach (var linha in linhas)
        {
            if (linha.Codigo.Trim().Length == 0)
                continue;

            if (texto.Length > 0)
                texto.Append("\r\n");

            texto.Append(linha.Codigo.Trim());
            texto.Append('\t');
            texto.Append('\t');
            texto.Append(linha.Percentual.Trim());
            texto.Append('\t');
            texto.Append(Limpar(linha.Nome));
        }

        return texto.ToString();
    }

    private static string Limpar(string? nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            return "";

        return nome.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ').Trim();
    }
}
