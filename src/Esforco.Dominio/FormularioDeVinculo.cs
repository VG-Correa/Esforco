namespace Esforco.Dominio;

public enum OpcaoDeTempo
{
    MeiaHora = 0,
    UmaHora = 1,
    Outro = 2,
}

/// <summary>
/// O tempo do prazo como a janelinha mostra: 30 min, 1 h ou outro valor em minutos ou horas.
/// </summary>
public sealed record TempoDeVinculo(OpcaoDeTempo Opcao, int Quantidade = 0, bool EmHoras = false)
{
    public static TempoDeVinculo Padrao { get; } = new(OpcaoDeTempo.MeiaHora);

    public static TempoDeVinculo De(TimeSpan? duracao)
    {
        if (duracao is not TimeSpan tempo || tempo <= TimeSpan.Zero)
            return Padrao;
        if (tempo == TimeSpan.FromMinutes(30))
            return Padrao;
        if (tempo == TimeSpan.FromHours(1))
            return new TempoDeVinculo(OpcaoDeTempo.UmaHora);

        var emHoras = tempo.TotalMinutes >= 60 && tempo.TotalMinutes % 60 == 0;
        var quantidade = Math.Max(1, Math.Round(emHoras ? tempo.TotalHours : tempo.TotalMinutes));
        return new TempoDeVinculo(OpcaoDeTempo.Outro, (int)Math.Min(quantidade, int.MaxValue), emHoras);
    }
}

/// <summary>
/// As duas frases da janelinha sobre o escritório identificado. Alerta marca a troca que não muda o registro.
/// </summary>
public sealed record AvisoDeIdentificado(string Origem, string AoSalvar, bool Alerta);

/// <summary>
/// Como a janelinha de vínculo abre: o que cada campo mostra e se salvar altera um vínculo
/// que já existe (EmEdicao) ou cria outro. Copia diz que os campos vieram de um vínculo que
/// já terminou, e nesse caso salvar cria um novo.
/// </summary>
public sealed record FormularioDeVinculo(
    AlvoDeVinculo Alvo,
    long? EmEdicao,
    bool Copia,
    string Escritorio,
    long? CategoriaId,
    AlcanceDoVinculo Alcance,
    bool SoRegistro,
    ValidadeDoVinculo Validade,
    DateTimeOffset Inicio,
    TempoDeVinculo Tempo)
{
    public bool PodeJanela => Alvo.Titulo.Trim().Length > 0;

    public bool PodeAba => PodeJanela || Alvo.Endereco.Trim().Length > 0;

    public bool OfereceRegistro => Alvo.DoCalendario;

    /// <summary>
    /// Abre com o vínculo que o alvo já tem. Sem vínculo, ou se ele foi excluído, abre com o escritório
    /// que o Esforço identificou sozinho, ou em branco quando não identificou nenhum.
    /// </summary>
    public static FormularioDeVinculo Montar(
        AlvoDeVinculo alvo,
        IReadOnlyList<VinculoDeEscritorio> vinculos,
        DateTimeOffset agora,
        AlcanceDoVinculo? sugerido = null)
    {
        ArgumentNullException.ThrowIfNull(alvo);
        ArgumentNullException.ThrowIfNull(vinculos);
        if (alvo.VinculoId is long id && vinculos.FirstOrDefault(item => item.Id == id) is VinculoDeEscritorio atual)
            return Editar(alvo, atual);
        if (alvo.CopiarDe is long de && vinculos.FirstOrDefault(item => item.Id == de) is VinculoDeEscritorio antigo)
            return Copiar(alvo, antigo, agora);
        return Novo(alvo, agora, sugerido);
    }

    public static FormularioDeVinculo Novo(AlvoDeVinculo alvo, DateTimeOffset agora, AlcanceDoVinculo? sugerido = null)
    {
        ArgumentNullException.ThrowIfNull(alvo);
        var limpo = alvo with { VinculoId = null, CopiarDe = null };
        var alcance = sugerido
            ?? (limpo.Endereco.Trim().Length > 0
                ? AlcanceDoVinculo.Aba
                : limpo.Titulo.Trim().Length > 0 ? AlcanceDoVinculo.Janela : AlcanceDoVinculo.Programa);
        return new FormularioDeVinculo(
            limpo,
            null,
            false,
            limpo.Identificado?.Id ?? "",
            null,
            Caber(alcance, limpo),
            limpo.DoCalendario,
            ValidadeDoVinculo.Sempre,
            limpo.Inicio ?? agora,
            TempoDeVinculo.Padrao);
    }

    /// <summary>
    /// O escritório que já vem escolhido porque o Esforço o identificou sozinho, e não por um vínculo salvo.
    /// </summary>
    public EscritorioIdentificado? Identificado => EmEdicao is null && !Copia ? Alvo.Identificado : null;

    /// <summary>
    /// Verdadeiro quando a janelinha abriu com o escritório identificado e a pessoa não mexeu em nenhum
    /// campo do vínculo. Salvar assim não cria vínculo: ele só repetiria o que o Esforço já sabe.
    /// </summary>
    public bool SoRepeteOIdentificado(
        string? escritorio,
        string? categoria,
        AlcanceDoVinculo alcance,
        bool soRegistro,
        ValidadeDoVinculo validade) =>
        Identificado is EscritorioIdentificado achado
        && string.Equals((escritorio ?? "").Trim(), achado.Id, StringComparison.Ordinal)
        && string.IsNullOrWhiteSpace(categoria)
        && alcance == Alcance
        && soRegistro == SoRegistro
        && (soRegistro || validade == Validade);

    /// <summary>
    /// O que a janelinha diz sobre o escritório identificado: a frase fixa de onde ele veio e, conforme
    /// o que está marcado, o que Salvar vai fazer. Sem escritório identificado, nada é dito.
    /// </summary>
    public AvisoDeIdentificado? Aviso(
        string rotuloDoIdentificado,
        string? escritorio,
        string rotuloDoEscolhido,
        string? categoria,
        AlcanceDoVinculo alcance,
        bool soRegistro,
        ValidadeDoVinculo validade)
    {
        if (Identificado is not EscritorioIdentificado achado)
            return null;

        var origem = $"Identificado pelo Esforço, {TextoDeVinculo.Fonte(achado.Fonte)}: {rotuloDoIdentificado}. Não é um vínculo salvo.";
        var marcado = (escritorio ?? "").Trim();
        if (marcado.Length == 0)
            return new AvisoDeIdentificado(origem, "", false);

        if (SoRepeteOIdentificado(marcado, categoria, alcance, soRegistro, validade))
        {
            return new AvisoDeIdentificado(
                origem,
                "Salvar assim não cria vínculo, porque o escritório já é este. A contagem do tempo é gravada se você mudou.",
                false);
        }

        if (!achado.VenceVinculo)
        {
            return new AvisoDeIdentificado(
                origem,
                "Salvar cria um vínculo, e o vínculo vale mais que a regra do título.",
                false);
        }

        if (string.Equals(marcado, achado.Id, StringComparison.Ordinal))
        {
            return new AvisoDeIdentificado(
                origem,
                "Salvar cria um vínculo, mas neste registro nada muda: o vínculo só vale onde o Esforço não identificar o escritório sozinho.",
                false);
        }

        return new AvisoDeIdentificado(
            origem,
            $"Este registro continua em {rotuloDoIdentificado}: o que o Esforço identifica {TextoDeVinculo.Fonte(achado.Fonte)} vale mais que o vínculo. "
            + $"O vínculo com {rotuloDoEscolhido} só vai valer onde o Esforço não identificar o escritório sozinho.",
            true);
    }

    /// <summary>
    /// O vínculo aberto pela tela de vínculos salvos, sem janela nem faixa por trás.
    /// </summary>
    public static FormularioDeVinculo DoVinculo(VinculoDeEscritorio vinculo)
    {
        ArgumentNullException.ThrowIfNull(vinculo);
        return Editar(new AlvoDeVinculo(vinculo.Programa, vinculo.Titulo, vinculo.Endereco), vinculo);
    }

    /// <summary>
    /// Programa, título e endereço saem do vínculo; o que ele não guarda vem da janela ou da faixa,
    /// para o alcance poder ser trocado. O registro marcado sozinho mantém o período dele.
    /// </summary>
    public static FormularioDeVinculo Editar(AlvoDeVinculo alvo, VinculoDeEscritorio vinculo)
    {
        ArgumentNullException.ThrowIfNull(alvo);
        ArgumentNullException.ThrowIfNull(vinculo);
        var junto = new AlvoDeVinculo(
            vinculo.Programa,
            vinculo.Titulo.Length > 0 ? vinculo.Titulo : alvo.Titulo,
            vinculo.Endereco.Length > 0 ? vinculo.Endereco : alvo.Endereco,
            vinculo.SoRegistro ? vinculo.Inicio : alvo.Inicio,
            vinculo.SoRegistro ? vinculo.Inicio + (vinculo.Duracao ?? TimeSpan.Zero) : alvo.Fim,
            alvo.Identificado,
            vinculo.Id);
        return new FormularioDeVinculo(
            junto,
            vinculo.Id,
            false,
            vinculo.Escritorio,
            vinculo.CategoriaId,
            Caber(vinculo.Alcance, junto),
            vinculo.SoRegistro,
            vinculo.SoRegistro ? ValidadeDoVinculo.Sempre : vinculo.Validade,
            vinculo.Inicio,
            vinculo.SoRegistro ? TempoDeVinculo.Padrao : TempoDeVinculo.De(vinculo.Duracao));
    }

    public static FormularioDeVinculo Copiar(AlvoDeVinculo alvo, VinculoDeEscritorio vinculo, DateTimeOffset agora)
    {
        ArgumentNullException.ThrowIfNull(alvo);
        ArgumentNullException.ThrowIfNull(vinculo);
        var limpo = alvo with { VinculoId = null, CopiarDe = vinculo.Id };
        return new FormularioDeVinculo(
            limpo,
            null,
            true,
            vinculo.Escritorio,
            vinculo.CategoriaId,
            Caber(vinculo.Alcance, limpo),
            false,
            vinculo.SoRegistro ? ValidadeDoVinculo.Sempre : vinculo.Validade,
            limpo.Inicio ?? agora,
            vinculo.SoRegistro ? TempoDeVinculo.Padrao : TempoDeVinculo.De(vinculo.Duracao));
    }

    private static AlcanceDoVinculo Caber(AlcanceDoVinculo alcance, AlvoDeVinculo alvo)
    {
        var temTitulo = alvo.Titulo.Trim().Length > 0;
        var temEndereco = alvo.Endereco.Trim().Length > 0;
        return alcance switch
        {
            AlcanceDoVinculo.Janela when !temTitulo => AlcanceDoVinculo.Programa,
            AlcanceDoVinculo.Aba when !temTitulo && !temEndereco => AlcanceDoVinculo.Programa,
            _ => alcance,
        };
    }
}
