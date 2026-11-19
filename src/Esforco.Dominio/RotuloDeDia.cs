using System.Globalization;

namespace Esforco.Dominio;

public static class RotuloDeDia
{
    private static readonly string[] Nomes = ["dom.", "seg.", "ter.", "qua.", "qui.", "sex.", "sáb."];

    public static string De(DateTimeOffset instante, DateTimeOffset agora)
    {
        var local = instante.ToLocalTime();
        var nome = Nomes[(int)local.DayOfWeek];
        var hoje = agora.ToLocalTime();
        var data = local.Year == hoje.Year
            ? local.ToString("dd/MM", CultureInfo.InvariantCulture)
            : local.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        return $"{nome} {data}";
    }
}
