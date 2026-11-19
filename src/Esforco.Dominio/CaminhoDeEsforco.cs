namespace Esforco.Dominio;

public static class CaminhoDeEsforco
{
    public static IReadOnlyList<string> De(
        IntervaloDeEsforco intervalo,
        DateTimeOffset agora,
        NomesDePainel? nomes = null)
    {
        var caminho = new List<string> { RotuloDeDia.De(intervalo.Inicio, agora) };
        if (!intervalo.EhPainelSplitC)
        {
            var contexto = string.IsNullOrWhiteSpace(intervalo.Contexto) ? "Sem título" : intervalo.Contexto;
            caminho.Add(intervalo.Programa);
            caminho.Add(contexto);
            return caminho;
        }

        caminho.AddRange(DoPainel(
            intervalo.Escritorio,
            intervalo.Plano,
            intervalo.Periodo,
            intervalo.Sessao,
            intervalo.Item,
            intervalo.TipoDoItem,
            nomes ?? NomesDePainel.Vazio));
        return caminho;
    }

    public static IReadOnlyList<string> DoPainel(LocalDoPainel local) =>
        DoPainel(local.Escritorio, local.Plano, local.Periodo, local.Sessao, local.Item, local.TipoDoItem, NomesDePainel.Vazio);

    private static IReadOnlyList<string> DoPainel(
        string escritorio,
        string plano,
        string periodo,
        string sessao,
        string item,
        TipoDeItem tipo,
        NomesDePainel nomes)
    {
        var caminho = new List<string> { InterpretadorDePainelSplitC.NomeDoPrograma };
        if (escritorio.Length > 0)
            caminho.Add(nomes.RotuloDeEscritorio(escritorio));
        if (plano.Length > 0)
            caminho.Add(nomes.RotuloDePlano(escritorio, plano));
        if (periodo.Length > 0)
            caminho.Add($"Período {periodo}");
        if (sessao.Length > 0)
            caminho.Add(sessao);
        if (item.Length > 0)
            caminho.Add(tipo == TipoDeItem.Dashboard ? $"Dashboard {item}" : $"Aba {item}");
        return caminho;
    }
}
