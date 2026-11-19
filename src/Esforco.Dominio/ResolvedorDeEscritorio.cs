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
    public static bool Descoberto(IntervaloDeEsforco intervalo, NomesDePainel nomes)
    {
        ArgumentNullException.ThrowIfNull(intervalo);
        ArgumentNullException.ThrowIfNull(nomes);
        if ((intervalo.EhPainelSplitC || intervalo.EhChatwoot) && intervalo.Escritorio.Trim().Length > 0)
            return true;

        var reuniao = InterpretadorDeReuniao.Ler(intervalo.Titulo);
        return reuniao is not null && nomes.IdDaReuniao(reuniao.Nome) is not null;
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
