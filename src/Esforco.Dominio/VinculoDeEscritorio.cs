namespace Esforco.Dominio;

public enum AlcanceDoVinculo
{
    Programa = 0,
    Janela = 1,
    Aba = 2,
}

public enum ValidadeDoVinculo
{
    Sempre = 0,
    Prazo = 1,
    Inatividade = 2,
}

/// <summary>
/// O que a janelinha de vínculo descreve: a janela da frente ou a faixa clicada no calendário.
/// Inicio e Fim só vêm do calendário.
/// </summary>
public sealed record AlvoDeVinculo(
    string Programa,
    string Titulo,
    string Endereco = "",
    DateTimeOffset? Inicio = null,
    DateTimeOffset? Fim = null,
    bool Descoberto = false)
{
    public bool DoCalendario => Inicio is not null && Fim is not null;
}

public sealed record PedidoDeVinculo(
    string Escritorio,
    long? CategoriaId,
    AlcanceDoVinculo Alcance,
    AlvoDeVinculo Alvo,
    ValidadeDoVinculo Validade,
    DateTimeOffset Inicio,
    TimeSpan? Duracao,
    bool SoRegistro = false,
    string NovaCategoria = "");

/// <summary>
/// Liga um programa, uma janela ou uma aba a um escritório. Nada do que já foi gravado muda:
/// o vínculo é aplicado na hora de montar o relatório e o calendário.
/// </summary>
public sealed record VinculoDeEscritorio(
    long Id,
    string Escritorio,
    long? CategoriaId,
    AlcanceDoVinculo Alcance,
    string Programa,
    string Titulo,
    string Endereco,
    ValidadeDoVinculo Validade,
    DateTimeOffset Inicio,
    TimeSpan? Duracao,
    bool SoRegistro,
    DateTimeOffset CriadoEm,
    DateTimeOffset? EncerradoEm = null)
{
    public static readonly TimeSpan MaiorDuracao = TimeSpan.FromDays(3650);

    public bool Temporario => Validade != ValidadeDoVinculo.Sempre;

    public bool Casa(IntervaloDeEsforco intervalo, string? endereco)
    {
        ArgumentNullException.ThrowIfNull(intervalo);
        if (!Igual(intervalo.Programa, Programa))
            return false;

        return Alcance switch
        {
            AlcanceDoVinculo.Programa => true,
            AlcanceDoVinculo.Janela => Titulo.Length > 0 && Igual(intervalo.Titulo, Titulo),
            AlcanceDoVinculo.Aba =>
                (Endereco.Length > 0 && !string.IsNullOrEmpty(endereco) && Igual(endereco, Endereco))
                || (Titulo.Length > 0 && Igual(intervalo.Titulo, Titulo)),
            _ => false,
        };
    }

    public static string? Impedir(PedidoDeVinculo pedido)
    {
        ArgumentNullException.ThrowIfNull(pedido);
        if ((pedido.Escritorio ?? "").Trim().Length == 0)
            return "Escolha o escritório.";

        var alvo = pedido.Alvo;
        if ((alvo.Programa ?? "").Trim().Length == 0)
            return "Não há programa para vincular.";

        var titulo = (alvo.Titulo ?? "").Trim();
        var endereco = EnderecoDeAba.Normalizar(alvo.Endereco);
        if (pedido.Alcance == AlcanceDoVinculo.Janela && titulo.Length == 0)
            return "Esta janela não tem título. Vincule o programa inteiro.";

        if (pedido.Alcance == AlcanceDoVinculo.Aba && titulo.Length == 0 && endereco.Length == 0)
            return "Esta aba não tem título nem endereço. Vincule o programa inteiro.";

        if (pedido.SoRegistro)
        {
            return alvo.DoCalendario
                ? null
                : "Só um registro do calendário pode ser vinculado sozinho.";
        }

        if (pedido.Validade == ValidadeDoVinculo.Sempre)
            return null;

        if (pedido.Duracao is not TimeSpan duracao || duracao <= TimeSpan.Zero)
            return pedido.Validade == ValidadeDoVinculo.Prazo
                ? "Informe o prazo."
                : "Informe quanto tempo sem voltar encerra o vínculo.";

        return duracao > MaiorDuracao ? "Esse tempo é longo demais. Use Sempre." : null;
    }

    public static VinculoDeEscritorio De(PedidoDeVinculo pedido, long? categoria, DateTimeOffset agora, long id = 0)
    {
        ArgumentNullException.ThrowIfNull(pedido);
        var aviso = Impedir(pedido);
        if (aviso is not null)
            throw new ArgumentException(aviso, nameof(pedido));

        var alvo = pedido.Alvo;
        var titulo = pedido.Alcance == AlcanceDoVinculo.Programa ? "" : (alvo.Titulo ?? "").Trim();
        var endereco = pedido.Alcance == AlcanceDoVinculo.Aba ? EnderecoDeAba.Normalizar(alvo.Endereco) : "";
        if (pedido.SoRegistro)
        {
            var inicio = alvo.Inicio!.Value;
            var duracao = alvo.Fim!.Value - inicio;
            if (duracao < TimeSpan.FromSeconds(1))
                duracao = TimeSpan.FromSeconds(1);

            return new VinculoDeEscritorio(
                id,
                pedido.Escritorio.Trim(),
                categoria,
                pedido.Alcance,
                alvo.Programa.Trim(),
                titulo,
                endereco,
                ValidadeDoVinculo.Prazo,
                inicio,
                duracao,
                true,
                agora);
        }

        return new VinculoDeEscritorio(
            id,
            pedido.Escritorio.Trim(),
            categoria,
            pedido.Alcance,
            alvo.Programa.Trim(),
            titulo,
            endereco,
            pedido.Validade,
            pedido.Inicio,
            pedido.Validade == ValidadeDoVinculo.Sempre ? null : pedido.Duracao,
            false,
            agora);
    }

    private static bool Igual(string? um, string? outro) =>
        string.Equals((um ?? "").Trim(), (outro ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
}
