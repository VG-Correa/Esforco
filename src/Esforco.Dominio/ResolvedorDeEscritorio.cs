namespace Esforco.Dominio;

public static class ResolvedorDeEscritorio
{
    public static string Resolver(string? titulo, IReadOnlyList<RegraDeEscritorio>? regras)
    {
        if (string.IsNullOrWhiteSpace(titulo) || regras is null || regras.Count == 0)
            return "";

        foreach (var regra in regras)
        {
            var nome = (regra.Escritorio ?? "").Trim();
            if (nome.Length == 0 || regra.Contem is null)
                continue;

            foreach (var trecho in regra.Contem)
            {
                var texto = (trecho ?? "").Trim();
                if (texto.Length < 2)
                    continue;

                if (titulo.Contains(texto, StringComparison.OrdinalIgnoreCase))
                    return nome;
            }
        }

        return "";
    }
}
