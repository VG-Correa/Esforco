namespace Esforco.Dominio;

public enum ModoDeContagem
{
    SoEmFoco = 0,
    EnquantoAberta = 1,
}

public enum AlcanceDaContagem
{
    Janela = 0,
    Programa = 1,
}

/// <summary>
/// Uma janela que existe agora, na frente ou não. Navegador diz que ela pode esconder abas.
/// Abas traz os nomes das abas dela quando foram lidos; nulo quando não foram.
/// PodeFaltarAba avisa que a lista lida pode não trazer todas, como quando há grupo de abas recolhido.
/// </summary>
public sealed record JanelaAberta(
    string Programa,
    string Titulo,
    bool Minimizada,
    bool Navegador = false,
    IReadOnlyList<string>? Abas = null,
    bool PodeFaltarAba = false);

/// <summary>
/// Como o tempo de uma janela, ou de um programa inteiro, é contado daqui para a frente.
/// Sem regra, a chamada de reunião conta enquanto está aberta e o resto conta só em foco.
/// </summary>
public sealed record ContagemDeJanela(
    long Id,
    AlcanceDaContagem Alcance,
    string Programa,
    string Titulo,
    ModoDeContagem Modo,
    DateTimeOffset SalvaEm)
{
    public bool Casa(string? programa, string? titulo) =>
        ContagemDaJanela.Igual(programa, Programa)
        && (Alcance == AlcanceDaContagem.Programa || ContagemDaJanela.MesmoTitulo(titulo, Titulo));
}

public sealed record ContagemEfetiva(ModoDeContagem Modo, ContagemDeJanela? Regra);

public enum AcaoDeContagem
{
    Nada = 0,
    Gravar = 1,
    Remover = 2,
}

public sealed record PlanoDeContagem(AcaoDeContagem Acao, ContagemDeJanela? Regra = null)
{
    public static PlanoDeContagem Nada { get; } = new(AcaoDeContagem.Nada);
}

public static class ContagemDaJanela
{
    public static ModoDeContagem Padrao(string? titulo) =>
        InterpretadorDeReuniao.EhChamada(titulo) ? ModoDeContagem.EnquantoAberta : ModoDeContagem.SoEmFoco;

    /// <summary>
    /// A regra da janela vale mais que a do programa, e a do programa vale mais que o padrão.
    /// </summary>
    public static ContagemEfetiva Achar(string? programa, string? titulo, IReadOnlyList<ContagemDeJanela> regras)
    {
        ArgumentNullException.ThrowIfNull(regras);
        var regra = DoAlcance(programa, titulo, AlcanceDaContagem.Janela, regras)
            ?? DoAlcance(programa, titulo, AlcanceDaContagem.Programa, regras);
        return regra is null ? new ContagemEfetiva(Padrao(titulo), null) : new ContagemEfetiva(regra.Modo, regra);
    }

    public static ContagemDeJanela? DoAlcance(
        string? programa,
        string? titulo,
        AlcanceDaContagem alcance,
        IReadOnlyList<ContagemDeJanela> regras)
    {
        ArgumentNullException.ThrowIfNull(regras);
        if (alcance == AlcanceDaContagem.Janela && (titulo ?? "").Trim().Length == 0)
            return null;

        return regras
            .Where(regra => regra.Alcance == alcance && regra.Casa(programa, titulo))
            .OrderByDescending(regra => regra.Id)
            .FirstOrDefault();
    }

    /// <summary>
    /// O que gravar quando a pessoa salva a escolha. Nada é gravado se a escolha é a que já valia.
    /// "Só em foco" para o programa inteiro, fora de reunião, é o mesmo que não ter regra do programa,
    /// então a regra sai em vez de ficar guardada e tirar a contagem aberta das reuniões dele.
    /// </summary>
    public static PlanoDeContagem Decidir(
        string? programa,
        string? titulo,
        AlcanceDaContagem alcance,
        ModoDeContagem modo,
        IReadOnlyList<ContagemDeJanela> regras,
        DateTimeOffset agora)
    {
        ArgumentNullException.ThrowIfNull(regras);
        var nome = (programa ?? "").Trim();
        var janela = (titulo ?? "").Trim();
        if (nome.Length == 0 || (alcance == AlcanceDaContagem.Janela && janela.Length == 0))
            return PlanoDeContagem.Nada;

        var existente = DoAlcance(nome, janela, alcance, regras);
        if (existente is not null && existente.Modo == modo)
            return PlanoDeContagem.Nada;

        var semEfeito = alcance == AlcanceDaContagem.Programa
            && modo == ModoDeContagem.SoEmFoco
            && InterpretadorDeReuniao.Ler(janela) is null;
        if (semEfeito)
            return existente is null ? PlanoDeContagem.Nada : new PlanoDeContagem(AcaoDeContagem.Remover, existente);

        if (existente is not null)
            return new PlanoDeContagem(AcaoDeContagem.Gravar, existente with { Modo = modo, SalvaEm = agora });

        if (alcance == AlcanceDaContagem.Janela && Achar(nome, janela, regras).Modo == modo)
            return PlanoDeContagem.Nada;

        return new PlanoDeContagem(
            AcaoDeContagem.Gravar,
            new ContagemDeJanela(0, alcance, nome, alcance == AlcanceDaContagem.Programa ? "" : janela, modo, agora));
    }

    public static bool Igual(string? um, string? outro) =>
        string.Equals((um ?? "").Trim(), (outro ?? "").Trim(), StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// A reunião tem o mesmo nome e tema na aba e na janelinha flutuante do Meet, com títulos diferentes.
    /// </summary>
    public static bool MesmoTitulo(string? um, string? outro)
    {
        if ((um ?? "").Trim().Length == 0 || (outro ?? "").Trim().Length == 0)
            return false;

        return Igual(um, outro) || InterpretadorDeReuniao.Mesma(um, outro);
    }
}

/// <summary>
/// Como a escolha de contagem abre na janelinha: o que vale agora para a janela e de onde vem.
/// </summary>
public sealed record FormularioDeContagem(
    string Programa,
    string Titulo,
    ModoDeContagem Modo,
    AlcanceDaContagem Alcance,
    bool Salva,
    ModoDeContagem? ModoDaJanela,
    ModoDeContagem? ModoDoPrograma)
{
    public bool PodeJanela => Titulo.Trim().Length > 0;

    public bool Reuniao => InterpretadorDeReuniao.EhChamada(Titulo);

    /// <summary>
    /// O que está salvo para o alcance, para a janelinha mostrar quando a pessoa troca de alcance.
    /// Nulo quando não há nada salvo: aí a escolha que está marcada fica como está.
    /// </summary>
    public ModoDeContagem? ModoSalvoEm(AlcanceDaContagem alcance) =>
        alcance == AlcanceDaContagem.Janela ? ModoDaJanela : ModoDoPrograma;

    public static FormularioDeContagem Montar(string? programa, string? titulo, IReadOnlyList<ContagemDeJanela> regras)
    {
        ArgumentNullException.ThrowIfNull(regras);
        var nome = (programa ?? "").Trim();
        var janela = (titulo ?? "").Trim();
        var efetiva = ContagemDaJanela.Achar(nome, janela, regras);
        var alcance = efetiva.Regra?.Alcance
            ?? (janela.Length > 0 ? AlcanceDaContagem.Janela : AlcanceDaContagem.Programa);
        return new FormularioDeContagem(
            nome,
            janela,
            efetiva.Modo,
            alcance,
            efetiva.Regra is not null,
            ContagemDaJanela.DoAlcance(nome, janela, AlcanceDaContagem.Janela, regras)?.Modo,
            ContagemDaJanela.DoAlcance(nome, janela, AlcanceDaContagem.Programa, regras)?.Modo);
    }
}
