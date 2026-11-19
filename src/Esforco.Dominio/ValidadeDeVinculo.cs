namespace Esforco.Dominio;

public sealed record JanelaDeValidade(DateTimeOffset Inicio, DateTimeOffset? Fim)
{
    public bool Vazia => Fim is DateTimeOffset fim && fim <= Inicio;

    public bool Vale(DateTimeOffset instante) =>
        instante >= Inicio && (Fim is null || instante < Fim.Value);

    public bool Cobre(DateTimeOffset de, DateTimeOffset ate) =>
        de >= Inicio && (Fim is null || ate <= Fim.Value);
}

/// <summary>
/// Diz de quando até quando um vínculo vale. O prazo fixo sai da conta direta.
/// A expiração por inatividade é lida do histórico, sem gravar nada.
/// </summary>
public static class ValidadeDeVinculo
{
    public static JanelaDeValidade Calcular(
        VinculoDeEscritorio vinculo,
        IEnumerable<IntervaloDeEsforco> historico,
        IReadOnlyDictionary<long, string>? enderecos,
        DateTimeOffset agora)
    {
        ArgumentNullException.ThrowIfNull(vinculo);
        ArgumentNullException.ThrowIfNull(historico);
        DateTimeOffset? fim = vinculo.Validade switch
        {
            ValidadeDoVinculo.Prazo => vinculo.Inicio + (vinculo.Duracao ?? TimeSpan.Zero),
            ValidadeDoVinculo.Inatividade => FimPorInatividade(vinculo, historico, enderecos, agora),
            _ => null,
        };

        if (vinculo.EncerradoEm is DateTimeOffset encerrado && (fim is null || encerrado < fim.Value))
            fim = encerrado;

        if (fim is DateTimeOffset limite && limite < vinculo.Inicio)
            fim = vinculo.Inicio;

        return new JanelaDeValidade(vinculo.Inicio, fim);
    }

    private static DateTimeOffset? FimPorInatividade(
        VinculoDeEscritorio vinculo,
        IEnumerable<IntervaloDeEsforco> historico,
        IReadOnlyDictionary<long, string>? enderecos,
        DateTimeOffset agora)
    {
        var limite = vinculo.Duracao ?? TimeSpan.Zero;
        var ultima = vinculo.Inicio;
        var visitas = historico
            .Where(intervalo => (intervalo.Fim ?? agora) > vinculo.Inicio)
            .Where(intervalo => vinculo.Casa(intervalo, Endereco(intervalo, enderecos)))
            .OrderBy(intervalo => intervalo.Inicio);

        foreach (var visita in visitas)
        {
            var comeco = visita.Inicio < vinculo.Inicio ? vinculo.Inicio : visita.Inicio;
            if (comeco - ultima > limite)
                return ultima + limite;

            var fim = visita.Fim ?? agora;
            if (fim > ultima)
                ultima = fim;
        }

        return agora - ultima > limite ? ultima + limite : null;
    }

    internal static string Endereco(IntervaloDeEsforco intervalo, IReadOnlyDictionary<long, string>? enderecos) =>
        enderecos is not null && intervalo.Id is long id && enderecos.TryGetValue(id, out var endereco)
            ? endereco
            : "";
}
