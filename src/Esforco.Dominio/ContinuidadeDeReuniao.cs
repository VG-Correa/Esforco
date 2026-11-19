namespace Esforco.Dominio;

public static class ContinuidadeDeReuniao
{
    public static readonly TimeSpan Folga = TimeSpan.FromHours(1);

    public static IReadOnlyList<IntervaloDeEsforco> Unir(IEnumerable<IntervaloDeEsforco> intervalos)
    {
        ArgumentNullException.ThrowIfNull(intervalos);
        var outros = new List<IntervaloDeEsforco>();
        var reunioes = new List<IntervaloDeEsforco>();
        foreach (var intervalo in intervalos.OrderBy(item => item.Inicio).ThenBy(item => item.Id))
        {
            if (InterpretadorDeReuniao.Ler(intervalo.Titulo) is null)
                outros.Add(intervalo);
            else
                reunioes.Add(intervalo);
        }

        IntervaloDeEsforco? atual = null;
        foreach (var reuniao in reunioes)
        {
            if (atual is null || !Cabe(atual, reuniao))
            {
                atual = Copiar(reuniao);
                outros.Add(atual);
                continue;
            }

            atual.ContinuarAte(reuniao.Fim);
        }

        return outros;
    }

    private static bool Cabe(IntervaloDeEsforco atual, IntervaloDeEsforco proxima) =>
        !proxima.Retomado
        && InterpretadorDeReuniao.Mesma(atual.Titulo, proxima.Titulo)
        && (atual.Fim is null || proxima.Inicio <= atual.Fim.Value + Folga);

    private static IntervaloDeEsforco Copiar(IntervaloDeEsforco origem)
    {
        var copia = IntervaloDeEsforco.Abrir(
            origem.Programa,
            origem.Titulo,
            origem.Contexto,
            origem.Escritorio,
            origem.Minimizada,
            origem.Inicio);
        if (origem.Retomado)
            copia.MarcarRetomado();
        if (origem.Fim is DateTimeOffset fim)
            copia.Fechar(fim);
        return copia;
    }
}
