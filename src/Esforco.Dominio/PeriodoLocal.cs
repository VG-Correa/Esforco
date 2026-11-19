namespace Esforco.Dominio;

public static class PeriodoLocal
{
    public static (DateTimeOffset Inicio, DateTimeOffset Fim) Hoje(DateTimeOffset agora)
    {
        var inicio = InicioDoDia(agora);
        return (inicio, inicio.AddDays(1));
    }

    public static (DateTimeOffset Inicio, DateTimeOffset Fim) Ontem(DateTimeOffset agora)
    {
        var hoje = InicioDoDia(agora);
        return (hoje.AddDays(-1), hoje);
    }

    public static (DateTimeOffset Inicio, DateTimeOffset Fim) Semana(DateTimeOffset agora)
    {
        var hoje = InicioDoDia(agora);
        var desdeSegunda = ((int)hoje.DayOfWeek + 6) % 7;
        var inicio = hoje.AddDays(-desdeSegunda);
        return (inicio, inicio.AddDays(7));
    }

    public static (DateTimeOffset Inicio, DateTimeOffset Fim) EntreDias(DateTime primeiro, DateTime ultimo)
    {
        var inicio = Dia(primeiro);
        var fim = Dia(ultimo).AddDays(1);
        if (fim <= inicio)
            throw new ArgumentOutOfRangeException(nameof(ultimo), "A data final precisa ser a mesma ou vir depois da inicial.");

        return (inicio, fim);
    }

    public static DateTimeOffset InicioDoDia(DateTimeOffset agora)
    {
        var local = agora.ToLocalTime();
        return new DateTimeOffset(local.Year, local.Month, local.Day, 0, 0, 0, local.Offset);
    }

    private static DateTimeOffset Dia(DateTime data)
    {
        var local = DateTime.SpecifyKind(data.Date, DateTimeKind.Unspecified);
        return new DateTimeOffset(local, TimeZoneInfo.Local.GetUtcOffset(local));
    }
}
