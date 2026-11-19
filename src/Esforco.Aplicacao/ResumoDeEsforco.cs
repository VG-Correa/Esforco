using Esforco.Dominio;

namespace Esforco.Aplicacao;

public sealed record ResumoDeEsforco(
    bool EmLeitura,
    IReadOnlyList<NoDeEsforco> Nos,
    IReadOnlyDictionary<string, PreferenciaDePrograma> Preferencias,
    bool TemRegistros);
