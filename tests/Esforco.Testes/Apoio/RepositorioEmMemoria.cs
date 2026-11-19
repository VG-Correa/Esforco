using Esforco.Aplicacao;
using Esforco.Dominio;

namespace Esforco.Testes.Apoio;

internal sealed class RepositorioEmMemoria : IRepositorioDeIntervalos
{
    private readonly List<IntervaloDeEsforco> _itens = new();
    private readonly Dictionary<string, PreferenciaDePrograma> _preferencias = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _nomes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<string>> _variacoes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Dictionary<string, string>> _planos = new(StringComparer.Ordinal);
    private readonly List<OcultacaoDeEscritorio> _ocultacoes = [];
    private readonly object _trava = new();
    private long _proximo = 1;

    public int Vistos { get; private set; }

    public IntervaloDeEsforco? ObterAberto()
    {
        lock (_trava)
            return _itens.LastOrDefault(item => item.Fim is null && !item.EmSegundoPlano);
    }

    public IntervaloDeEsforco? ObterReuniaoEmSegundoPlano()
    {
        lock (_trava)
            return _itens.LastOrDefault(item => item.Fim is null && item.EmSegundoPlano);
    }

    public void DefinirSegundoPlano(IntervaloDeEsforco intervalo, bool emSegundoPlano)
    {
        ArgumentNullException.ThrowIfNull(intervalo);
        intervalo.DefinirSegundoPlano(emSegundoPlano);
    }

    public void Adicionar(IntervaloDeEsforco intervalo)
    {
        lock (_trava)
        {
            intervalo.DefinirId(_proximo++);
            _itens.Add(intervalo);
        }
    }

    public void SalvarFechamento(IntervaloDeEsforco intervalo)
    {
        lock (_trava)
        {
            if (intervalo.Id is null || intervalo.Fim is null)
                throw new InvalidOperationException("O intervalo precisa estar fechado e gravado.");

            var existente = _itens.FirstOrDefault(item => item.Id == intervalo.Id)
                ?? throw new InvalidOperationException("Intervalo não encontrado.");

            if (!ReferenceEquals(existente, intervalo))
                existente.Fechar(intervalo.Fim.Value);
        }
    }

    public void MarcarVisto(long id, DateTimeOffset instante)
    {
        lock (_trava)
        {
            if (_itens.Any(item => item.Id == id && item.Fim is null))
                Vistos++;
        }
    }

    public IReadOnlyList<IntervaloDeEsforco> Listar()
    {
        lock (_trava)
            return _itens.OrderBy(item => item.Inicio).ThenBy(item => item.Id).ToList();
    }

    public IReadOnlyList<PreferenciaDePrograma> ListarPreferencias()
    {
        lock (_trava)
            return _preferencias.Values.ToList();
    }

    public void SalvarPreferencia(PreferenciaDePrograma preferencia)
    {
        lock (_trava)
        {
            if (!preferencia.TemAlguma)
                _preferencias.Remove(preferencia.Programa);
            else
                _preferencias[preferencia.Programa] = preferencia;
        }
    }

    public void ExcluirPrograma(string programa)
    {
        lock (_trava)
        {
            _itens.RemoveAll(item => string.Equals(item.Programa, programa, StringComparison.Ordinal));
            _preferencias.Remove(programa);
        }
    }

    public void ExcluirIntervalo(long id)
    {
        lock (_trava)
            _itens.RemoveAll(item => item.Id == id);
    }

    public void AtualizarLimites(IntervaloDeEsforco intervalo)
    {
        lock (_trava)
        {
            if (intervalo.Id is null || !_itens.Any(item => item.Id == intervalo.Id))
                throw new InvalidOperationException("Intervalo não encontrado.");
        }
    }

    public IReadOnlyList<CadastroDeEscritorio> ListarCadastros()
    {
        lock (_trava)
        {
            var ids = new HashSet<string>(_nomes.Keys, StringComparer.Ordinal);
            ids.UnionWith(_variacoes.Keys);
            ids.UnionWith(_planos.Keys);
            return ids
                .OrderBy(id => id, StringComparer.Ordinal)
                .Select(id => new CadastroDeEscritorio(
                    id,
                    _nomes.GetValueOrDefault(id) ?? "",
                    _variacoes.GetValueOrDefault(id) ?? [],
                    _planos.TryGetValue(id, out var planos)
                        ? planos.OrderBy(item => item.Key, StringComparer.Ordinal).Select(item => new NomeDePlano(item.Key, item.Value)).ToList()
                        : []))
                .ToList();
        }
    }

    public IReadOnlyList<OcultacaoDeEscritorio> ListarOcultacoes()
    {
        lock (_trava)
            return _ocultacoes.ToList();
    }

    public void AdicionarOcultacao(OcultacaoDeEscritorio ocultacao)
    {
        ArgumentNullException.ThrowIfNull(ocultacao);
        lock (_trava)
        {
            if (!_ocultacoes.Contains(ocultacao))
                _ocultacoes.Add(ocultacao);
        }
    }

    public void RemoverOcultacao(OcultacaoDeEscritorio ocultacao)
    {
        ArgumentNullException.ThrowIfNull(ocultacao);
        lock (_trava)
            _ocultacoes.Remove(ocultacao);
    }

    public void DefinirNomeDeEscritorio(string id, string nome)
    {
        lock (_trava)
        {
            if (nome.Length == 0)
                _nomes.Remove(id);
            else
                _nomes[id] = nome;
        }
    }

    public void DefinirNomeDePlano(string escritorio, string plano, string nome)
    {
        lock (_trava)
        {
            if (!_planos.TryGetValue(escritorio, out var planos))
            {
                if (nome.Length == 0)
                    return;

                planos = new Dictionary<string, string>(StringComparer.Ordinal);
                _planos[escritorio] = planos;
            }

            if (nome.Length == 0)
            {
                planos.Remove(plano);
                if (planos.Count == 0)
                    _planos.Remove(escritorio);
                return;
            }

            planos[plano] = nome;
        }
    }

    public void AdicionarVariacao(string escritorio, string variacao)
    {
        lock (_trava)
        {
            if (!_variacoes.TryGetValue(escritorio, out var lista))
            {
                lista = [];
                _variacoes[escritorio] = lista;
            }

            if (!lista.Contains(variacao, StringComparer.Ordinal))
                lista.Add(variacao);
        }
    }

    public void RemoverVariacao(string escritorio, string variacao)
    {
        lock (_trava)
        {
            if (!_variacoes.TryGetValue(escritorio, out var lista))
                return;

            lista.RemoveAll(item => item == variacao);
            if (lista.Count == 0)
                _variacoes.Remove(escritorio);
        }
    }
}
