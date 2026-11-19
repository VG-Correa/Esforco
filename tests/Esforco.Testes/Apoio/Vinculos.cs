using Esforco.Dominio;

namespace Esforco.Testes.Apoio;

internal static class Vinculos
{
    public static readonly TimeSpan Zona = TimeSpan.FromHours(-3);

    public static DateTimeOffset Em(int dia, int hora, int minuto) =>
        new(2026, 11, dia, hora, minuto, 0, Zona);

    public static VinculoDeEscritorio Novo(
        long id,
        string escritorio,
        AlcanceDoVinculo alcance,
        string programa,
        string titulo = "",
        string endereco = "",
        ValidadeDoVinculo validade = ValidadeDoVinculo.Sempre,
        DateTimeOffset? inicio = null,
        TimeSpan? duracao = null,
        long? categoria = null,
        bool soRegistro = false,
        DateTimeOffset? criadoEm = null,
        DateTimeOffset? encerradoEm = null) =>
        new(
            id,
            escritorio,
            categoria,
            alcance,
            programa,
            titulo,
            endereco,
            validade,
            inicio ?? Em(1, 0, 0),
            duracao,
            soRegistro,
            criadoEm ?? Em(1, 0, 0).AddMinutes(id),
            encerradoEm);

    public static ContextoDeVinculos Contexto(
        IEnumerable<VinculoDeEscritorio> vinculos,
        IEnumerable<Categoria>? categorias = null,
        IReadOnlyDictionary<long, string>? enderecos = null) =>
        new(vinculos.ToList(), categorias?.ToList() ?? [], enderecos ?? new Dictionary<long, string>());

    public static IntervaloDeEsforco Janela(
        string programa,
        string tela,
        DateTimeOffset inicio,
        DateTimeOffset? fim,
        long? id = null)
    {
        var intervalo = IntervaloDeEsforco.Abrir(programa, tela + " - " + programa, tela, "", false, inicio);
        if (id is long numero)
            intervalo.DefinirId(numero);
        if (fim is DateTimeOffset termino)
            intervalo.Fechar(termino);
        return intervalo;
    }

    public static IntervaloDeEsforco Reuniao(string titulo, DateTimeOffset inicio, DateTimeOffset fim)
    {
        var intervalo = IntervaloDeEsforco.Abrir("Google Chrome", titulo, titulo, "", false, inicio);
        intervalo.Fechar(fim);
        return intervalo;
    }

    public static IntervaloDeEsforco Painel(string escritorio, DateTimeOffset inicio, DateTimeOffset fim)
    {
        var intervalo = IntervaloDeEsforco.AbrirPainel(
            "SplitC",
            new LocalDoPainel(escritorio, "17927", "", "Fontes", "862247", TipoDeItem.Aba),
            false,
            inicio);
        intervalo.Fechar(fim);
        return intervalo;
    }
}
