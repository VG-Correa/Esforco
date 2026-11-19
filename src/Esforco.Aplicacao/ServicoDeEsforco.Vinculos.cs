using Esforco.Dominio;

namespace Esforco.Aplicacao;

public sealed record EscritorioComNome(string Id, string Nome);

public sealed record VinculoSalvo(
    VinculoDeEscritorio Vinculo,
    string Escritorio,
    string Categoria,
    string Alcance,
    string Validade,
    bool Ativo);

public sealed partial class ServicoDeEsforco
{
    private string? _tituloDaJanelinha;
    private AmostraDeJanela? _segura;
    private AmostraDeJanela? _ultimaAmostra;

    /// <summary>
    /// Chamado antes de abrir a janelinha de vínculo. Guarda a janela que está na frente para que
    /// o tempo siga contando para ela enquanto a janelinha estiver aberta, e devolve o que será vinculado.
    /// Com a leitura parada não lê nada e devolve nulo.
    /// </summary>
    public AlvoDeVinculo? SegurarJanelaDaFrente(string tituloDaJanelinha)
    {
        lock (_gate)
        {
            _tituloDaJanelinha = (tituloDaJanelinha ?? "").Trim();
            _segura = null;
            if (!_emLeitura || _suspenso)
                return null;
        }

        AmostraDeJanela? lida = null;
        if (Ocupar())
        {
            try
            {
                lida = _leitor.Ler();
            }
            catch (Exception)
            {
                lida = null;
            }
            finally
            {
                Interlocked.Exchange(ref _ocupado, 0);
            }
        }

        lock (_gate)
        {
            if (!_emLeitura || _suspenso || _tituloDaJanelinha is null)
                return null;

            var amostra = lida is null || EhJanelinha(lida) ? _ultimaAmostra : lida;
            if (amostra is null)
                return null;

            _segura = amostra;
            return AlvoDe(amostra);
        }
    }

    public void SoltarJanelaDaFrente()
    {
        lock (_gate)
        {
            _tituloDaJanelinha = null;
            _segura = null;
        }
    }

    public IReadOnlyList<EscritorioComNome> ListarEscritoriosComNome() =>
        _repositorio.ListarCadastros()
            .Where(cadastro => cadastro.Id.Length > 0 && cadastro.Nome.Trim().Length > 0)
            .Select(cadastro => new EscritorioComNome(cadastro.Id, cadastro.Nome.Trim()))
            .OrderBy(item => item.Nome, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Id, StringComparer.Ordinal)
            .ToList();

    public ResultadoDeVinculo Vincular(PedidoDeVinculo pedido)
    {
        ArgumentNullException.ThrowIfNull(pedido);
        if (_vinculos is null)
            return new ResultadoDeVinculo(null, "O cadastro de vínculos não está disponível.");

        var resultado = _vinculos.Criar(pedido);
        if (resultado.Vinculo is not { Alcance: AlcanceDoVinculo.Aba } vinculo || vinculo.Endereco.Length == 0)
            return resultado;

        if (pedido.Alvo.DoCalendario)
            return resultado;

        lock (_gate)
        {
            var aberto = _repositorio.ObterAberto();
            if (aberto?.Id is long id
                && string.Equals(aberto.Programa, vinculo.Programa, StringComparison.OrdinalIgnoreCase)
                && string.Equals(aberto.Titulo.Trim(), (pedido.Alvo.Titulo ?? "").Trim(), StringComparison.OrdinalIgnoreCase))
            {
                _vinculos.GuardarEndereco(id, vinculo.Endereco);
            }
        }

        return resultado;
    }

    public IReadOnlyList<VinculoSalvo> ConsultarVinculos()
    {
        if (_vinculos is null)
            return [];

        var contexto = _vinculos.Contexto();
        if (contexto.Vinculos.Count == 0)
            return [];

        var agora = _relogio.Agora();
        var historico = ContinuidadeDeReuniao.Unir(_repositorio.Listar());
        var nomes = new NomesDePainel(_repositorio.ListarCadastros());
        var categorias = contexto.Categorias.ToDictionary(item => item.Id, item => item.Nome);
        return contexto.Vinculos
            .Select(vinculo =>
            {
                var janela = ValidadeDeVinculo.Calcular(vinculo, historico, contexto.Enderecos, agora);
                var categoria = vinculo.CategoriaId is long id && categorias.TryGetValue(id, out var nome) ? nome : "";
                return new VinculoSalvo(
                    vinculo,
                    nomes.RotuloDeEscritorio(vinculo.Escritorio),
                    categoria,
                    TextoDeVinculo.Alcance(vinculo),
                    TextoDeVinculo.Validade(vinculo, janela, agora),
                    TextoDeVinculo.Ativo(janela, agora));
            })
            .OrderByDescending(item => item.Ativo)
            .ThenByDescending(item => item.Vinculo.CriadoEm)
            .ThenByDescending(item => item.Vinculo.Id)
            .ToList();
    }

    private bool EhJanelinha(AmostraDeJanela amostra) =>
        amostra.Propria
        && _tituloDaJanelinha is string titulo
        && string.Equals((amostra.Titulo ?? "").Trim(), titulo, StringComparison.Ordinal);

    private AlvoDeVinculo AlvoDe(AmostraDeJanela amostra)
    {
        var titulo = (amostra.Titulo ?? "").Trim();
        var endereco = (amostra.Endereco ?? "").Trim();
        var conversa = (amostra.Conversa ?? "").Trim();
        if (conversa.Length > 200)
            conversa = conversa[..200];

        var local = InterpretadorDePainelSplitC.Interpretar(endereco);
        var nome = NomeDoPrograma(amostra.Programa, local, titulo, endereco);
        var agora = _relogio.Agora();
        var rascunho = local is null
            ? IntervaloDeEsforco.Abrir(
                nome,
                titulo,
                Contexto(titulo, endereco, conversa),
                Escritorio(titulo, endereco, conversa),
                amostra.Minimizada,
                agora)
            : IntervaloDeEsforco.AbrirPainel(titulo, local, amostra.Minimizada, agora);
        var descoberto = ResolvedorDeEscritorio.Descoberto(rascunho, new NomesDePainel(_repositorio.ListarCadastros()));
        return new AlvoDeVinculo(nome, titulo, local is null ? EnderecoDeAba.Normalizar(endereco) : "", Descoberto: descoberto);
    }

    /// <summary>
    /// Espera a leitura em curso terminar, para não ler a janela da frente em dois lugares ao mesmo tempo.
    /// </summary>
    private bool Ocupar()
    {
        for (var tentativa = 0; tentativa < 100; tentativa++)
        {
            if (Interlocked.CompareExchange(ref _ocupado, 1, 0) == 0)
                return true;
            Thread.Sleep(20);
        }

        return false;
    }

    private void LimparEnderecosSemIntervalo()
    {
        if (_vinculos is null || _vinculos.Contexto().Enderecos.Count == 0)
            return;

        var vivos = new HashSet<long>();
        foreach (var intervalo in _repositorio.Listar())
        {
            if (intervalo.Id is long id)
                vivos.Add(id);
        }

        _vinculos.ManterEnderecos(vivos);
    }
}
