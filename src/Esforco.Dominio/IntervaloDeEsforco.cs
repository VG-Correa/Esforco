namespace Esforco.Dominio;

public sealed class IntervaloDeEsforco
{
    private IntervaloDeEsforco(
        long? id,
        string programa,
        string titulo,
        string contexto,
        string escritorio,
        string plano,
        string periodo,
        string sessao,
        string item,
        TipoDeItem tipoDoItem,
        bool minimizada,
        bool emSegundoPlano,
        DateTimeOffset inicio,
        DateTimeOffset? fim,
        bool retomado = false)
    {
        Id = id;
        Programa = programa;
        Titulo = titulo;
        Contexto = contexto;
        Escritorio = escritorio;
        Plano = plano;
        Periodo = periodo;
        Sessao = sessao;
        Item = item;
        TipoDoItem = tipoDoItem;
        Minimizada = minimizada;
        EmSegundoPlano = emSegundoPlano;
        Retomado = retomado;
        Inicio = inicio;
        Fim = fim;
    }

    public long? Id { get; private set; }

    public string Programa { get; }

    public string Titulo { get; }

    public string Contexto { get; }

    public string Escritorio { get; }

    public string Plano { get; }

    public string Periodo { get; }

    public string Sessao { get; }

    public string Item { get; }

    public TipoDeItem TipoDoItem { get; }

    public bool EhPainelSplitC =>
        string.Equals(Programa, InterpretadorDePainelSplitC.NomeDoPrograma, StringComparison.Ordinal);

    public bool EhChatwoot =>
        string.Equals(Programa, InterpretadorDeChatwoot.NomeDoPrograma, StringComparison.Ordinal)
        || InterpretadorDeChatwoot.EhAba(Titulo, null);

    public bool Minimizada { get; }

    public bool EmSegundoPlano { get; private set; }

    public bool DuranteReuniao { get; private set; }

    public bool Retomado { get; private set; }

    /// <summary>
    /// Nome do escritório ligado por vínculo manual. Só vive em memória, para a tela.
    /// </summary>
    public string EscritorioVinculado { get; private set; } = "";

    public DateTimeOffset Inicio { get; private set; }

    public DateTimeOffset? Fim { get; private set; }

    public static IntervaloDeEsforco Abrir(
        string? programa,
        string? titulo,
        string? contexto,
        string? escritorio,
        bool minimizada,
        DateTimeOffset inicio,
        bool emSegundoPlano = false)
    {
        var nome = (programa ?? "").Trim();
        if (nome.Length == 0)
            nome = "Programa desconhecido";

        return new IntervaloDeEsforco(
            null,
            nome,
            (titulo ?? "").Trim(),
            (contexto ?? "").Trim(),
            (escritorio ?? "").Trim(),
            "",
            "",
            "",
            "",
            TipoDeItem.Nenhum,
            minimizada,
            emSegundoPlano,
            inicio,
            null);
    }

    public static IntervaloDeEsforco AbrirPainel(
        string? titulo,
        LocalDoPainel local,
        bool minimizada,
        DateTimeOffset inicio)
    {
        ArgumentNullException.ThrowIfNull(local);
        var caminho = CaminhoDeEsforco.DoPainel(local);
        var contexto = caminho.Count > 1 ? caminho[^1] : "";

        return new IntervaloDeEsforco(
            null,
            InterpretadorDePainelSplitC.NomeDoPrograma,
            (titulo ?? "").Trim(),
            contexto,
            local.Escritorio.Trim(),
            local.Plano.Trim(),
            local.Periodo.Trim(),
            local.Sessao.Trim(),
            local.Item.Trim(),
            local.TipoDoItem,
            minimizada,
            false,
            inicio,
            null);
    }

    public static IntervaloDeEsforco Restaurar(
        long id,
        string programa,
        string titulo,
        string contexto,
        string escritorio,
        string plano,
        string periodo,
        string sessao,
        string item,
        TipoDeItem tipoDoItem,
        bool minimizada,
        DateTimeOffset inicio,
        DateTimeOffset? fim,
        bool emSegundoPlano = false,
        bool retomado = false)
    {
        return new IntervaloDeEsforco(
            id,
            programa,
            titulo,
            contexto,
            escritorio,
            plano,
            periodo,
            sessao,
            item,
            tipoDoItem,
            minimizada,
            emSegundoPlano,
            inicio,
            fim,
            retomado);
    }

    public void DefinirSegundoPlano(bool emSegundoPlano) => EmSegundoPlano = emSegundoPlano;

    public void MarcarRetomado() => Retomado = true;

    public void MarcarDuranteReuniao(bool duranteReuniao) => DuranteReuniao = duranteReuniao;

    public void MarcarVinculo(string? escritorio) => EscritorioVinculado = (escritorio ?? "").Trim();

    /// <summary>
    /// Cópia limitada a um pedaço deste intervalo. O intervalo gravado não muda.
    /// </summary>
    public IntervaloDeEsforco Recortar(DateTimeOffset inicio, DateTimeOffset? fim)
    {
        var comeco = inicio < Inicio ? Inicio : inicio;
        var termino = fim;
        if (Fim is DateTimeOffset limite && (termino is null || termino.Value > limite))
            termino = limite;
        if (termino is DateTimeOffset ate && ate < comeco)
            termino = comeco;

        var copia = new IntervaloDeEsforco(
            Id,
            Programa,
            Titulo,
            Contexto,
            Escritorio,
            Plano,
            Periodo,
            Sessao,
            Item,
            TipoDoItem,
            Minimizada,
            EmSegundoPlano,
            comeco,
            termino,
            Retomado);
        copia.DuranteReuniao = DuranteReuniao;
        return copia;
    }

    public void ContinuarAte(DateTimeOffset? fim)
    {
        if (fim is null || Fim is null)
        {
            Fim = null;
            return;
        }

        if (fim > Fim)
            Fim = fim;
    }

    public void DefinirId(long id)
    {
        if (id <= 0)
            throw new ArgumentOutOfRangeException(nameof(id));
        if (Id is not null)
            throw new InvalidOperationException("O intervalo já tem identificador.");
        Id = id;
    }

    public bool Continua(string? programa, string? titulo, bool minimizada)
    {
        if (Fim is not null)
            return false;

        return string.Equals(Programa, (programa ?? "").Trim(), StringComparison.Ordinal)
            && string.Equals(Titulo, (titulo ?? "").Trim(), StringComparison.Ordinal)
            && Minimizada == minimizada;
    }

    public bool MesmoLocal(LocalDoPainel local)
    {
        ArgumentNullException.ThrowIfNull(local);
        return EhPainelSplitC
            && Escritorio == local.Escritorio
            && Plano == local.Plano
            && Periodo == local.Periodo
            && Sessao == local.Sessao
            && Item == local.Item
            && TipoDoItem == local.TipoDoItem;
    }

    public void Fechar(DateTimeOffset fim)
    {
        if (Fim is not null)
            return;

        Fim = fim < Inicio ? Inicio : fim;
    }

    public void EncurtarFim(DateTimeOffset fim)
    {
        var limite = fim < Inicio ? Inicio : fim;
        if (Fim is null || limite < Fim.Value)
            Fim = limite;
    }

    public void AdiantarInicio(DateTimeOffset inicio)
    {
        if (inicio <= Inicio)
            return;

        var teto = Fim ?? DateTimeOffset.MaxValue;
        Inicio = inicio > teto ? teto : inicio;
    }

    public IntervaloDeEsforco SepararDepois(DateTimeOffset inicio)
    {
        var limite = Fim is DateTimeOffset fim && inicio > fim ? fim : inicio;
        return new IntervaloDeEsforco(
            null,
            Programa,
            Titulo,
            Contexto,
            Escritorio,
            Plano,
            Periodo,
            Sessao,
            Item,
            TipoDoItem,
            Minimizada,
            EmSegundoPlano,
            limite,
            Fim,
            Retomado);
    }

    public TimeSpan DuracaoAte(DateTimeOffset agora)
    {
        var fim = Fim ?? agora;
        if (fim < Inicio)
            return TimeSpan.Zero;
        return fim - Inicio;
    }
}
