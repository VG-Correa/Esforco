namespace Esforco.Dominio;

public sealed record PlanoDeCorte(
    IReadOnlyList<long> Excluir,
    IReadOnlyList<IntervaloDeEsforco> Atualizar,
    IReadOnlyList<IntervaloDeEsforco> Criar);

public static class CorteDePeriodo
{
    public static PlanoDeCorte Planejar(
        IEnumerable<IntervaloDeEsforco> intervalos,
        DateTimeOffset inicio,
        DateTimeOffset fim,
        DateTimeOffset agora)
    {
        ArgumentNullException.ThrowIfNull(intervalos);
        if (fim <= inicio)
            throw new ArgumentOutOfRangeException(nameof(fim), "O fim do período precisa ser depois do início.");

        var excluir = new List<long>();
        var atualizar = new List<IntervaloDeEsforco>();
        var criar = new List<IntervaloDeEsforco>();

        foreach (var intervalo in intervalos)
        {
            if (intervalo.Id is not long id)
                continue;

            var fimEfetivo = intervalo.Fim ?? agora;
            if (fimEfetivo < intervalo.Inicio)
                fimEfetivo = intervalo.Inicio;

            var sobrepoe = intervalo.Inicio < fim && fimEfetivo > inicio;
            if (!sobrepoe)
                continue;

            var comecaAntes = intervalo.Inicio < inicio;
            var terminaDepois = fimEfetivo > fim;

            if (!comecaAntes && !terminaDepois)
            {
                excluir.Add(id);
                continue;
            }

            if (comecaAntes && terminaDepois)
            {
                var continuacao = intervalo.SepararDepois(fim);
                intervalo.EncurtarFim(inicio);
                atualizar.Add(intervalo);
                criar.Add(continuacao);
                continue;
            }

            if (comecaAntes)
                intervalo.EncurtarFim(inicio);
            else
                intervalo.AdiantarInicio(fim);

            atualizar.Add(intervalo);
        }

        return new PlanoDeCorte(excluir, atualizar, criar);
    }
}
