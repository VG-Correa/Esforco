namespace Esforco.Dominio;

public static class InterpretadorDePainelSplitC
{
    public const string NomeDoPrograma = "Painel SplitC";

    public static LocalDoPainel? Interpretar(string? endereco)
    {
        if (string.IsNullOrWhiteSpace(endereco))
            return null;

        if (!TentarUri(endereco.Trim(), out var uri))
            return null;

        if (!uri.Host.Equals("app.splitc.com.br", StringComparison.OrdinalIgnoreCase))
            return null;

        var segmentos = uri.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(Uri.UnescapeDataString)
            .ToArray();

        var plano = "";
        var aba = "";
        if (segmentos.Length > 0 && segmentos[0].Equals("planos", StringComparison.OrdinalIgnoreCase))
        {
            if (segmentos.Length >= 2 && !segmentos[1].Equals("tab", StringComparison.OrdinalIgnoreCase))
                plano = segmentos[1];

            var indiceTab = Array.FindIndex(segmentos, parte => parte.Equals("tab", StringComparison.OrdinalIgnoreCase));
            if (indiceTab >= 0 && indiceTab + 1 < segmentos.Length)
                aba = segmentos[indiceTab + 1];
        }

        var consulta = Consulta(uri.Query);
        consulta.TryGetValue("officeId", out var escritorio);
        consulta.TryGetValue("periodId", out var periodo);
        consulta.TryGetValue("tabSelector", out var seletor);
        consulta.TryGetValue("panelId", out var painel);

        var (sessao, item, tipo) = Classificar(seletor ?? "", aba, painel ?? "", plano);
        return new LocalDoPainel(
            (escritorio ?? "").Trim(),
            plano.Trim(),
            (periodo ?? "").Trim(),
            sessao,
            item.Trim(),
            tipo);
    }

    private static (string Sessao, string Item, TipoDeItem Tipo) Classificar(
        string seletor,
        string aba,
        string painel,
        string plano)
    {
        switch (seletor.Trim().ToLowerInvariant())
        {
            case "source":
                return ("Fontes", aba, aba.Length > 0 ? TipoDeItem.Aba : TipoDeItem.Nenhum);
            case "plans":
                return ("Cálculos", aba, aba.Length > 0 ? TipoDeItem.Aba : TipoDeItem.Nenhum);
            case "validation":
                return ("Validação", "", TipoDeItem.Nenhum);
            case "statement":
                return painel.Length > 0
                    ? ("Dashboards", painel, TipoDeItem.Dashboard)
                    : ("Dashboards", "", TipoDeItem.Nenhum);
            case "payroll":
                return ("Usuários e pagamentos", aba, aba.Length > 0 ? TipoDeItem.Aba : TipoDeItem.Nenhum);
            case "graph":
                return ("Painel de controle", aba, aba.Length > 0 ? TipoDeItem.Aba : TipoDeItem.Nenhum);
            case "":
                if (plano.Length == 0 && aba.Length == 0)
                    return ("Lista de planos", "", TipoDeItem.Nenhum);
                return ("", aba, aba.Length > 0 ? TipoDeItem.Aba : TipoDeItem.Nenhum);
            default:
                return (seletor.Trim(), aba, aba.Length > 0 ? TipoDeItem.Aba : TipoDeItem.Nenhum);
        }
    }

    private static bool TentarUri(string texto, out Uri uri)
    {
        if (Uri.TryCreate(texto, UriKind.Absolute, out uri!))
            return true;

        if (texto.Contains("app.splitc.com.br", StringComparison.OrdinalIgnoreCase)
            && Uri.TryCreate("https://" + texto.TrimStart('/'), UriKind.Absolute, out uri!))
            return true;

        uri = null!;
        return false;
    }

    private static Dictionary<string, string> Consulta(string query)
    {
        var mapa = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(query))
            return mapa;

        var texto = query[0] == '?' ? query[1..] : query;
        foreach (var parte in texto.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var igual = parte.IndexOf('=');
            var chave = Uri.UnescapeDataString((igual < 0 ? parte : parte[..igual]).Replace('+', ' '));
            var valor = igual < 0
                ? ""
                : Uri.UnescapeDataString(parte[(igual + 1)..].Replace('+', ' ')).Trim();
            if (chave.Length > 0 && !mapa.ContainsKey(chave))
                mapa[chave] = valor;
        }

        return mapa;
    }
}
