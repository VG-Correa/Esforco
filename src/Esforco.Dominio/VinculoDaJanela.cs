namespace Esforco.Dominio;

/// <summary>
/// O vínculo que a janelinha mostra quando o atalho é usado em uma janela que já foi vinculada.
/// Terminou diz que ele não vale mais: nesse caso os campos são copiados e salvar cria outro,
/// para o período antigo continuar como estava.
/// </summary>
public sealed record VinculoDaJanela(VinculoDeEscritorio Vinculo, bool Terminou)
{
    public static VinculoDaJanela? Achar(
        string? programa,
        string? titulo,
        string? endereco,
        ContextoDeVinculos? contexto,
        IEnumerable<IntervaloDeEsforco> historico,
        DateTimeOffset agora)
    {
        ArgumentNullException.ThrowIfNull(historico);
        if (contexto is null || contexto.Vinculos.Count == 0)
            return null;

        var janela = IntervaloDeEsforco.Abrir(programa, titulo, "", "", false, agora);
        var normal = EnderecoDeAba.Normalizar(endereco);
        var todos = historico as IReadOnlyCollection<IntervaloDeEsforco> ?? historico.ToList();
        var valendo = new List<VinculoDeEscritorio>();
        var aguardando = new List<VinculoDeEscritorio>();
        VinculoDeEscritorio? ultimo = null;
        var fimDoUltimo = DateTimeOffset.MinValue;
        foreach (var vinculo in contexto.Vinculos)
        {
            if (vinculo.SoRegistro || !vinculo.Casa(janela, normal))
                continue;

            var validade = ValidadeDeVinculo.Calcular(vinculo, todos, contexto.Enderecos, agora);
            if (validade.Fim is DateTimeOffset fim && fim <= agora)
            {
                if (ultimo is null || fim > fimDoUltimo || (fim == fimDoUltimo && MaisNovo(vinculo, ultimo)))
                {
                    ultimo = vinculo;
                    fimDoUltimo = fim;
                }

                continue;
            }

            (validade.Inicio <= agora ? valendo : aguardando).Add(vinculo);
        }

        if (valendo.Count > 0)
            return new VinculoDaJanela(ResolvedorDeEscritorio.Escolher(valendo)!, false);
        if (aguardando.Count > 0)
            return new VinculoDaJanela(ResolvedorDeEscritorio.Escolher(aguardando)!, false);
        return ultimo is null ? null : new VinculoDaJanela(ultimo, true);
    }

    private static bool MaisNovo(VinculoDeEscritorio novo, VinculoDeEscritorio atual) =>
        novo.CriadoEm != atual.CriadoEm ? novo.CriadoEm > atual.CriadoEm : novo.Id > atual.Id;
}
