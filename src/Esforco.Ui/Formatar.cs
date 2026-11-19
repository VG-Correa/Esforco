using System.Globalization;
using Esforco.Dominio;

namespace Esforco.Ui;

public static class Formatar
{
    public static string Instante(DateTimeOffset instante) =>
        instante.ToLocalTime().ToString("dd/MM HH:mm:ss", CultureInfo.InvariantCulture);

    public static string Fim(DateTimeOffset? fim) =>
        fim is null ? "—" : Instante(fim.Value);

    public static string Duracao(TimeSpan valor) => TextoDeDuracao.De(valor);

    public static string Hora(DateTimeOffset instante) =>
        instante.ToLocalTime().ToString("HH:mm:ss", CultureInfo.InvariantCulture);

    public static string Faixa(DateTimeOffset inicio, DateTimeOffset? fim) =>
        fim is null ? $"{Hora(inicio)} – agora" : $"{Hora(inicio)} – {Hora(fim.Value)}";
}
