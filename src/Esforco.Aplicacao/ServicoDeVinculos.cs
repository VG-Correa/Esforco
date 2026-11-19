using Esforco.Dominio;

namespace Esforco.Aplicacao;

public sealed record ResultadoDeVinculo(VinculoDeEscritorio? Vinculo, string? Aviso);

public sealed record ResultadoDeCategoria(Categoria? Categoria, string? Aviso);

/// <summary>
/// Cadastro de categorias e de vínculos manuais, e o atalho que abre a janelinha.
/// </summary>
public sealed class ServicoDeVinculos
{
    public const string ChaveDoAtalho = "atalho_vinculo";

    private readonly IRepositorioDeVinculos _repositorio;
    private readonly IRelogio _relogio;
    private readonly object _gate = new();
    private ContextoDeVinculos? _contexto;
    private IReadOnlyList<ContagemDeJanela>? _contagens;

    public ServicoDeVinculos(IRepositorioDeVinculos repositorio, IRelogio relogio)
    {
        _repositorio = repositorio;
        _relogio = relogio;
    }

    /// <summary>
    /// Vínculos, categorias e endereços de uma vez. Fica em memória até a próxima gravação,
    /// porque a tela pede isso a cada segundo.
    /// </summary>
    public ContextoDeVinculos Contexto()
    {
        lock (_gate)
        {
            return _contexto ??= new ContextoDeVinculos(
                _repositorio.ListarVinculos(),
                _repositorio.ListarCategorias(),
                _repositorio.ListarEnderecos());
        }
    }

    public IReadOnlyList<Categoria> ListarCategorias() => Contexto().Categorias;

    public IReadOnlyList<VinculoDeEscritorio> ListarVinculos() => Contexto().Vinculos;

    /// <summary>
    /// Como a janelinha abre para a janela da frente ou para a faixa do calendário:
    /// com o vínculo que o alvo já tem, ou em branco.
    /// </summary>
    public FormularioDeVinculo Formulario(AlvoDeVinculo alvo, AlcanceDoVinculo? sugerido = null) =>
        FormularioDeVinculo.Montar(alvo, Contexto().Vinculos, _relogio.Agora(), sugerido);

    /// <summary>
    /// A janelinha em branco para o mesmo alvo, quando a pessoa quer outro vínculo em vez de alterar o que existe.
    /// </summary>
    public FormularioDeVinculo FormularioNovo(AlvoDeVinculo alvo, AlcanceDoVinculo? sugerido = null) =>
        FormularioDeVinculo.Novo(alvo, _relogio.Agora(), sugerido);

    public ResultadoDeCategoria CriarCategoria(string? nome)
    {
        lock (_gate)
        {
            var aviso = CadastroDeCategorias.ImpedirNome(nome, _repositorio.ListarCategorias());
            if (aviso is not null)
                return new ResultadoDeCategoria(null, aviso);

            var criada = _repositorio.AdicionarCategoria(NomesDePainel.Exibir(nome));
            _contexto = null;
            return new ResultadoDeCategoria(criada, null);
        }
    }

    public string? RenomearCategoria(long id, string? nome)
    {
        lock (_gate)
        {
            var categorias = _repositorio.ListarCategorias();
            if (categorias.All(item => item.Id != id))
                return "Essa categoria não existe mais.";

            var aviso = CadastroDeCategorias.ImpedirNome(nome, categorias, id);
            if (aviso is not null)
                return aviso;

            _repositorio.RenomearCategoria(id, NomesDePainel.Exibir(nome));
            _contexto = null;
            return null;
        }
    }

    /// <summary>
    /// Sem escolha, a categoria em uso não é excluída: o retorno diz em quantos vínculos ela está,
    /// para a tela perguntar se troca por outra ou tira a categoria deles.
    /// </summary>
    public ExclusaoDeCategoria ExcluirCategoria(long id, EscolhaAoExcluirCategoria? escolha = null)
    {
        lock (_gate)
        {
            var plano = CadastroDeCategorias.PlanejarExclusao(
                id,
                _repositorio.ListarCategorias(),
                _repositorio.ListarVinculos(),
                escolha);
            if (!plano.Pode)
                return plano;

            _repositorio.ExcluirCategoria(id, plano.TrocarPor);
            _contexto = null;
            return plano;
        }
    }

    public ResultadoDeVinculo Criar(PedidoDeVinculo pedido)
    {
        ArgumentNullException.ThrowIfNull(pedido);
        lock (_gate)
        {
            var aviso = Preparar(pedido, out var categoria);
            if (aviso is not null)
                return new ResultadoDeVinculo(null, aviso);

            var criado = _repositorio.AdicionarVinculo(VinculoDeEscritorio.De(pedido, categoria, _relogio.Agora()));
            _contexto = null;
            return new ResultadoDeVinculo(criado, null);
        }
    }

    public ResultadoDeVinculo Editar(long id, PedidoDeVinculo pedido)
    {
        ArgumentNullException.ThrowIfNull(pedido);
        lock (_gate)
        {
            var atual = _repositorio.ListarVinculos().FirstOrDefault(item => item.Id == id);
            if (atual is null)
                return new ResultadoDeVinculo(null, "Esse vínculo não existe mais.");

            var aviso = Preparar(pedido, out var categoria);
            if (aviso is not null)
                return new ResultadoDeVinculo(null, aviso);

            var novo = VinculoDeEscritorio.De(pedido, categoria, atual.CriadoEm, id) with
            {
                EncerradoEm = atual.EncerradoEm,
            };
            _repositorio.AtualizarVinculo(novo);
            _contexto = null;
            return new ResultadoDeVinculo(novo, null);
        }
    }

    public void EncerrarAgora(long id)
    {
        lock (_gate)
        {
            var atual = _repositorio.ListarVinculos().FirstOrDefault(item => item.Id == id);
            if (atual is null || atual.EncerradoEm is not null)
                return;

            _repositorio.AtualizarVinculo(atual with { EncerradoEm = _relogio.Agora() });
            _contexto = null;
        }
    }

    public void Excluir(long id)
    {
        lock (_gate)
        {
            _repositorio.ExcluirVinculo(id);
            _contexto = null;
        }
    }

    /// <summary>
    /// Guarda o endereço de um intervalo só quando existe vínculo de aba por esse endereço.
    /// Endereço de aba sem vínculo não é gravado.
    /// </summary>
    public void GuardarEnderecoSeVinculado(IntervaloDeEsforco intervalo, string? endereco)
    {
        ArgumentNullException.ThrowIfNull(intervalo);
        if (intervalo.Id is not long id)
            return;

        var normal = EnderecoDeAba.Normalizar(endereco);
        if (normal.Length == 0)
            return;

        lock (_gate)
        {
            var contexto = Contexto();
            var vinculado = false;
            foreach (var vinculo in contexto.Vinculos)
            {
                if (vinculo.Alcance == AlcanceDoVinculo.Aba
                    && vinculo.Endereco.Length > 0
                    && string.Equals(vinculo.Endereco, normal, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(vinculo.Programa, intervalo.Programa, StringComparison.OrdinalIgnoreCase))
                {
                    vinculado = true;
                    break;
                }
            }

            if (!vinculado)
                return;

            if (contexto.Enderecos.TryGetValue(id, out var guardado) && guardado == normal)
                return;

            _repositorio.GuardarEndereco(id, normal);
            _contexto = null;
        }
    }

    public void GuardarEndereco(long intervalo, string? endereco)
    {
        var normal = EnderecoDeAba.Normalizar(endereco);
        if (normal.Length == 0)
            return;

        lock (_gate)
        {
            _repositorio.GuardarEndereco(intervalo, normal);
            _contexto = null;
        }
    }

    public void ManterEnderecos(IReadOnlyCollection<long> intervalos)
    {
        ArgumentNullException.ThrowIfNull(intervalos);
        lock (_gate)
        {
            if (Contexto().Enderecos.Keys.All(intervalos.Contains))
                return;

            _repositorio.ManterEnderecos(intervalos);
            _contexto = null;
        }
    }

    /// <summary>
    /// As regras de contagem. Ficam em memória até a próxima gravação, porque a leitura pede a cada segundo.
    /// </summary>
    public IReadOnlyList<ContagemDeJanela> Contagens()
    {
        lock (_gate)
            return _contagens ??= _repositorio.ListarContagens();
    }

    /// <summary>
    /// Como a escolha de contagem abre na janelinha para essa janela: o que está salvo, ou o padrão.
    /// </summary>
    public FormularioDeContagem FormularioDaContagem(string? programa, string? titulo) =>
        FormularioDeContagem.Montar(programa, titulo, Contagens());

    /// <summary>
    /// Guarda como a janela, ou o programa inteiro, conta o tempo daqui para a frente.
    /// Devolve se algo mudou. Nenhum registro antigo é mexido.
    /// </summary>
    public bool SalvarContagem(string? programa, string? titulo, AlcanceDaContagem alcance, ModoDeContagem modo)
    {
        lock (_gate)
        {
            var plano = ContagemDaJanela.Decidir(programa, titulo, alcance, modo, Contagens(), _relogio.Agora());
            if (plano.Acao == AcaoDeContagem.Nada || plano.Regra is null)
                return false;

            if (plano.Acao == AcaoDeContagem.Remover)
                _repositorio.ExcluirContagem(plano.Regra.Id);
            else
                _repositorio.SalvarContagem(plano.Regra);

            _contagens = null;
            return true;
        }
    }

    public AtalhoDeTeclado LerAtalho()
    {
        lock (_gate)
            return AtalhoDeTeclado.Ler(_repositorio.LerConfiguracao(ChaveDoAtalho)) ?? AtalhoDeTeclado.Padrao;
    }

    public void DefinirAtalho(AtalhoDeTeclado atalho)
    {
        ArgumentNullException.ThrowIfNull(atalho);
        lock (_gate)
            _repositorio.GravarConfiguracao(ChaveDoAtalho, atalho.ToString());
    }

    private string? Preparar(PedidoDeVinculo pedido, out long? categoria)
    {
        categoria = pedido.CategoriaId;
        var aviso = VinculoDeEscritorio.Impedir(pedido);
        if (aviso is not null)
            return aviso;

        var categorias = _repositorio.ListarCategorias();
        var nova = NomesDePainel.Exibir(pedido.NovaCategoria);
        if (nova.Length > 0)
        {
            var existente = CadastroDeCategorias.ComNome(nova, categorias);
            if (existente is not null)
            {
                categoria = existente.Id;
                return null;
            }

            var impedimento = CadastroDeCategorias.ImpedirNome(nova, categorias);
            if (impedimento is not null)
                return impedimento;

            categoria = _repositorio.AdicionarCategoria(nova).Id;
            _contexto = null;
            return null;
        }

        if (categoria is long id && categorias.All(item => item.Id != id))
            return "A categoria escolhida não existe mais.";

        return null;
    }
}
