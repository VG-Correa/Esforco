using System.Globalization;

namespace Esforco.Dominio;

public static class TextoDeDuracao
{
    public static string De(TimeSpan valor)
    {
        if (valor < TimeSpan.Zero)
            valor = TimeSpan.Zero;

        var horas = (int)valor.TotalHours;
        return string.Create(CultureInfo.InvariantCulture, $"{horas:00}:{valor.Minutes:00}:{valor.Seconds:00}");
    }
}
