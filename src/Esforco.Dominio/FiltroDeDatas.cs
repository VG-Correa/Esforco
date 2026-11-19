namespace Esforco.Dominio;

public enum TipoDeFiltroDeDatas
{
    Dia,
    Semanal,
    Mensal,
    Trimestral,
    Semestral,
    Anual,
    Personalizado,
}

public static class FiltroDeDatas
{
    public static (DateTime Inicio, DateTime Fim) Do(TipoDeFiltroDeDatas tipo, DateTime referencia, bool deDomingoASabado)
    {
        var dia = referencia.Date;
        return tipo switch
        {
            TipoDeFiltroDeDatas.Semanal => Semana(dia, deDomingoASabado),
            TipoDeFiltroDeDatas.Mensal => Mes(dia),
            TipoDeFiltroDeDatas.Trimestral => Bloco(dia, 3),
            TipoDeFiltroDeDatas.Semestral => Bloco(dia, 6),
            TipoDeFiltroDeDatas.Anual => (new DateTime(dia.Year, 1, 1), new DateTime(dia.Year, 12, 31)),
            TipoDeFiltroDeDatas.Personalizado => (dia, dia),
            _ => (dia, dia),
        };
    }

    public static (DateTime Inicio, DateTime Fim) Deslocar(
        TipoDeFiltroDeDatas tipo,
        DateTime inicio,
        DateTime fim,
        int direcao,
        bool deDomingoASabado)
    {
        inicio = inicio.Date;
        fim = fim.Date;
        if (tipo == TipoDeFiltroDeDatas.Personalizado)
        {
            var dias = (fim - inicio).Days + 1;
            if (dias < 1)
                dias = 1;
            var passo = dias * direcao;
            return (inicio.AddDays(passo), fim.AddDays(passo));
        }

        var deslocada = tipo switch
        {
            TipoDeFiltroDeDatas.Semanal => inicio.AddDays(7 * direcao),
            TipoDeFiltroDeDatas.Mensal => inicio.AddMonths(direcao),
            TipoDeFiltroDeDatas.Trimestral => inicio.AddMonths(3 * direcao),
            TipoDeFiltroDeDatas.Semestral => inicio.AddMonths(6 * direcao),
            TipoDeFiltroDeDatas.Anual => inicio.AddYears(direcao),
            _ => inicio.AddDays(direcao),
        };
        return Do(tipo, deslocada, deDomingoASabado);
    }

    public static DateTime ReferenciaAoDeslocar(
        DateTime referencia,
        DateTime inicioAtual,
        DateTime novoInicio,
        DateTime novoFim)
    {
        inicioAtual = inicioAtual.Date;
        novoInicio = novoInicio.Date;
        novoFim = novoFim.Date;
        if (novoFim < novoInicio)
            (novoInicio, novoFim) = (novoFim, novoInicio);

        var offset = (referencia.Date - inicioAtual).Days;
        if (offset < 0)
            offset = 0;

        var dia = novoInicio.AddDays(offset);
        if (dia > novoFim)
            dia = novoFim;
        if (dia < novoInicio)
            dia = novoInicio;
        return dia;
    }

    private static (DateTime Inicio, DateTime Fim) Semana(DateTime dia, bool deDomingoASabado)
    {
        if (deDomingoASabado)
        {
            var domingo = dia.AddDays(-(int)dia.DayOfWeek);
            return (domingo, domingo.AddDays(6));
        }

        var segunda = dia.AddDays(-((int)dia.DayOfWeek + 6) % 7);
        return (segunda, segunda.AddDays(4));
    }

    private static (DateTime Inicio, DateTime Fim) Mes(DateTime dia)
    {
        var inicio = new DateTime(dia.Year, dia.Month, 1);
        return (inicio, inicio.AddMonths(1).AddDays(-1));
    }

    private static (DateTime Inicio, DateTime Fim) Bloco(DateTime dia, int meses)
    {
        var primeiroMes = ((dia.Month - 1) / meses) * meses + 1;
        var inicio = new DateTime(dia.Year, primeiroMes, 1);
        return (inicio, inicio.AddMonths(meses).AddDays(-1));
    }
}
