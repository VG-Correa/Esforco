namespace Esforco.Dominio;

public static class ResolvedorDeEscritorio
{
    public static string Resolver(string? titulo, IReadOnlyList<RegraDeEscritorio>? regras)
    {
        if (string.IsNullOrWhiteSpace(titulo) || regras is null || regras.Count == 0)
            return "";

        foreach (var regra in regras)
        {
            var nome = (regra.Escritorio ?? "").Trim();
            if (nome.Length == 0 || regra.Contem is null)
                continue;

            foreach (var trecho in regra.Contem)
            {
                var texto = (trecho ?? "").Trim();
                if (texto.Length < 2)
                    continue;

                if (titulo.Contains(texto, StringComparison.OrdinalIgnoreCase))
                    return nome;
            }
        }

        return "";
    }

    /// <summary>
    /// O que o programa descobre sozinho vence o vínculo manual:
    /// o officeId do Painel, o título do chat no Chatwoot e o título da reunião.
    /// </summary>
    public static bool Descoberto(IntervaloDeEsforco intervalo, NomesDePainel nomes) =>
        Identificar(intervalo, nomes) is { VenceVinculo: true };

    /// <summary>
    /// Qual escritório o programa já sabe sozinho para o registro, e por onde soube. Nulo quando não sabe:
    /// uma reunião cujo nome não é de nenhum escritório cadastrado fica sem escritório. A regra do título
    /// só conta quando leva a um escritório cadastrado, e é a única fonte que perde para o vínculo.
    /// </summary>
    public static EscritorioIdentificado? Identificar(IntervaloDeEsforco intervalo, NomesDePainel nomes)
    {
        ArgumentNullException.ThrowIfNull(intervalo);
        ArgumentNullException.ThrowIfNull(nomes);
        var campo = intervalo.Escritorio.Trim();
        var doPainel = intervalo.EhPainelSplitC || intervalo.EhChatwoot;
        if (doPainel && campo.Length > 0)
        {
            return new EscritorioIdentificado(
                campo,
                intervalo.EhPainelSplitC ? FonteDoEscritorio.Painel : FonteDoEscritorio.Chatwoot);
        }

        var reuniao = InterpretadorDeReuniao.Ler(intervalo.Titulo);
        if (reuniao is not null)
        {
            return nomes.IdDaReuniao(reuniao.Nome) is string daReuniao
                ? new EscritorioIdentificado(daReuniao, FonteDoEscritorio.Reuniao)
                : null;
        }

        if (doPainel || campo.Length == 0)
            return null;

        var daRegra = nomes.Conhece(campo) ? campo : nomes.IdDaReuniao(campo) ?? nomes.IdNoTexto(campo);
        return daRegra is null ? null : new EscritorioIdentificado(daRegra, FonteDoEscritorio.Regra);
    }

    /// <summary>
    /// Entre vínculos, o registro marcado no calendário vem primeiro. Depois o mais específico
    /// (aba, janela, programa), o temporário antes do permanente e, no empate, o mais novo.
    /// </summary>
    public static VinculoDeEscritorio? Escolher(IEnumerable<VinculoDeEscritorio> candidatos)
    {
        ArgumentNullException.ThrowIfNull(candidatos);
        VinculoDeEscritorio? escolhido = null;
        foreach (var candidato in candidatos)
        {
            if (escolhido is null || Vence(candidato, escolhido))
                escolhido = candidato;
        }

        return escolhido;
    }

    private static bool Vence(VinculoDeEscritorio novo, VinculoDeEscritorio atual)
    {
        if (novo.SoRegistro != atual.SoRegistro)
            return novo.SoRegistro;
        if (novo.Alcance != atual.Alcance)
            return novo.Alcance > atual.Alcance;
        if (novo.Temporario != atual.Temporario)
            return novo.Temporario;
        if (novo.CriadoEm != atual.CriadoEm)
            return novo.CriadoEm > atual.CriadoEm;
        return novo.Id > atual.Id;
    }
}
