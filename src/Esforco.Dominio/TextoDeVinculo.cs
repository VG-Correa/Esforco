using System.Globalization;

namespace Esforco.Dominio;

/// <summary>
/// Frases que descrevem um vínculo na tela de vínculos salvos.
/// </summary>
public static class TextoDeVinculo
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("pt-BR");

    public static string Alcance(VinculoDeEscritorio vinculo)
    {
        ArgumentNullException.ThrowIfNull(vinculo);
        return vinculo.Alcance switch
        {
            AlcanceDoVinculo.Janela => $"Janela ou arquivo: {vinculo.Titulo}",
            AlcanceDoVinculo.Aba when vinculo.Titulo.Length > 0 && vinculo.Endereco.Length > 0 =>
                $"Aba: {vinculo.Titulo} ou {vinculo.Endereco}",
            AlcanceDoVinculo.Aba => $"Aba: {(vinculo.Titulo.Length > 0 ? vinculo.Titulo : vinculo.Endereco)}",
            _ => "Programa inteiro",
        };
    }

    /// <summary>
    /// Por onde o Esforço identificou o escritório, para completar uma frase como "identificado ...".
    /// </summary>
    public static string Fonte(FonteDoEscritorio fonte) => fonte switch
    {
        FonteDoEscritorio.Painel => "pelo endereço do Painel",
        FonteDoEscritorio.Chatwoot => "pelo chat aberto no Chatwoot",
        FonteDoEscritorio.Reuniao => "pelo título da reunião",
        _ => "por uma regra do título",
    };

    public static string Validade(VinculoDeEscritorio vinculo, JanelaDeValidade janela, DateTimeOffset agora)
    {
        ArgumentNullException.ThrowIfNull(vinculo);
        ArgumentNullException.ThrowIfNull(janela);
        var desde = Instante(vinculo.Inicio);
        if (vinculo.SoRegistro)
            return $"Só o registro de {desde} a {Instante(janela.Fim ?? vinculo.Inicio)}";

        var encerrado = vinculo.EncerradoEm is DateTimeOffset fim && janela.Fim == fim;
        if (encerrado)
            return $"Encerrado em {Instante(vinculo.EncerradoEm!.Value)} (valia desde {desde})";

        switch (vinculo.Validade)
        {
            case ValidadeDoVinculo.Prazo:
                var limite = janela.Fim ?? vinculo.Inicio;
                return limite <= agora
                    ? $"Valeu de {desde} a {Instante(limite)}"
                    : $"De {desde} até {Instante(limite)}";
            case ValidadeDoVinculo.Inatividade:
                var espera = Duracao(vinculo.Duracao ?? TimeSpan.Zero);
                return janela.Fim is DateTimeOffset expirou
                    ? $"Expirou em {Instante(expirou)}, depois de {espera} sem voltar (valia desde {desde})"
                    : $"Desde {desde}, até ficar {espera} sem voltar";
            default:
                return vinculo.Inicio > agora ? $"Sempre, a partir de {desde}" : $"Sempre, desde {desde}";
        }
    }

    public static bool Ativo(JanelaDeValidade janela, DateTimeOffset agora)
    {
        ArgumentNullException.ThrowIfNull(janela);
        return janela.Fim is null || janela.Fim.Value > agora;
    }

    public static string Duracao(TimeSpan duracao)
    {
        if (duracao < TimeSpan.Zero)
            duracao = TimeSpan.Zero;

        var minutos = (long)Math.Round(duracao.TotalMinutes);
        if (minutos < 1)
            return $"{Math.Max(1, (long)Math.Round(duracao.TotalSeconds))} s";
        if (minutos < 60)
            return $"{minutos} min";

        var horas = minutos / 60;
        var resto = minutos % 60;
        return resto == 0 ? $"{horas} h" : $"{horas} h {resto} min";
    }

    private static string Instante(DateTimeOffset instante) =>
        instante.ToLocalTime().ToString("dd/MM/yyyy HH:mm", Cultura);
}
