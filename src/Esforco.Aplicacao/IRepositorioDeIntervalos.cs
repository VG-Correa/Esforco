using Esforco.Dominio;

namespace Esforco.Aplicacao;

public interface IRepositorioDeIntervalos
{
    IntervaloDeEsforco? ObterAberto();

    IntervaloDeEsforco? ObterReuniaoEmSegundoPlano();

    /// <summary>
    /// Tudo o que está contando sem estar na frente: a reunião e as janelas que contam enquanto abertas.
    /// </summary>
    IReadOnlyList<IntervaloDeEsforco> ListarEmSegundoPlano();

    void DefinirSegundoPlano(IntervaloDeEsforco intervalo, bool emSegundoPlano);

    void Adicionar(IntervaloDeEsforco intervalo);

    void SalvarFechamento(IntervaloDeEsforco intervalo);

    void MarcarVisto(long id, DateTimeOffset instante);

    IReadOnlyList<IntervaloDeEsforco> Listar();

    IReadOnlyList<PreferenciaDePrograma> ListarPreferencias();

    void SalvarPreferencia(PreferenciaDePrograma preferencia);

    void ExcluirPrograma(string programa);

    void ExcluirIntervalo(long id);

    void AtualizarLimites(IntervaloDeEsforco intervalo);

    IReadOnlyList<CadastroDeEscritorio> ListarCadastros();

    void DefinirNomeDeEscritorio(string id, string nome);

    void DefinirNomeDePlano(string escritorio, string plano, string nome);

    void AdicionarVariacao(string escritorio, string variacao);

    void RemoverVariacao(string escritorio, string variacao);

    IReadOnlyList<OcultacaoDeEscritorio> ListarOcultacoes();

    void AdicionarOcultacao(OcultacaoDeEscritorio ocultacao);

    void RemoverOcultacao(OcultacaoDeEscritorio ocultacao);
}
