using Esforco.Aplicacao;
using Esforco.Dominio;

namespace Esforco.Testes.Apoio;

internal sealed class RepositorioDeVinculosEmMemoria : IRepositorioDeVinculos
{
    private readonly List<Categoria> _categorias = [];
    private readonly List<VinculoDeEscritorio> _vinculos = [];
    private readonly Dictionary<long, string> _enderecos = [];
    private readonly Dictionary<string, string> _configuracoes = new(StringComparer.Ordinal);
    private long _proximaCategoria = 1;
    private long _proximoVinculo = 1;

    public IReadOnlyList<Categoria> ListarCategorias() => _categorias.ToList();

    public Categoria AdicionarCategoria(string nome)
    {
        var categoria = new Categoria(_proximaCategoria++, nome);
        _categorias.Add(categoria);
        return categoria;
    }

    public void RenomearCategoria(long id, string nome)
    {
        var indice = _categorias.FindIndex(item => item.Id == id);
        if (indice >= 0)
            _categorias[indice] = _categorias[indice] with { Nome = nome };
    }

    public void ExcluirCategoria(long id, long? trocarPor)
    {
        for (var i = 0; i < _vinculos.Count; i++)
        {
            if (_vinculos[i].CategoriaId == id)
                _vinculos[i] = _vinculos[i] with { CategoriaId = trocarPor };
        }

        _categorias.RemoveAll(item => item.Id == id);
    }

    public IReadOnlyList<VinculoDeEscritorio> ListarVinculos() => _vinculos.ToList();

    public VinculoDeEscritorio AdicionarVinculo(VinculoDeEscritorio vinculo)
    {
        var gravado = vinculo with { Id = _proximoVinculo++ };
        _vinculos.Add(gravado);
        return gravado;
    }

    public void AtualizarVinculo(VinculoDeEscritorio vinculo)
    {
        var indice = _vinculos.FindIndex(item => item.Id == vinculo.Id);
        if (indice >= 0)
            _vinculos[indice] = vinculo;
    }

    public void ExcluirVinculo(long id) => _vinculos.RemoveAll(item => item.Id == id);

    public IReadOnlyDictionary<long, string> ListarEnderecos() => new Dictionary<long, string>(_enderecos);

    public void GuardarEndereco(long intervalo, string endereco) => _enderecos[intervalo] = endereco;

    public void ManterEnderecos(IReadOnlyCollection<long> intervalos)
    {
        foreach (var id in _enderecos.Keys.Where(id => !intervalos.Contains(id)).ToList())
            _enderecos.Remove(id);
    }

    public string? LerConfiguracao(string chave) =>
        _configuracoes.TryGetValue(chave, out var valor) ? valor : null;

    public void GravarConfiguracao(string chave, string valor) => _configuracoes[chave] = valor;
}
