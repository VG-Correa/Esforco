using Esforco.Aplicacao;
using Esforco.Dominio;
using Esforco.Testes.Apoio;

namespace Esforco.Testes.Aplicacao;

/// <summary>
/// O atalho na janela cujo escritório o Esforço já identificou sozinho abre a janelinha com ele.
/// </summary>
public class JanelinhaComEscritorioIdentificadoTestes
{
    private const string Janelinha = "Escritório e contagem da janela";
    private const string ReuniaoDaStraumann = "Meet: Straumann LATAM <> SplitC - Google Chrome";

    [Fact]
    public void Atalho_na_reuniao_identificada_abre_com_o_escritorio_dela_e_sem_vinculo()
    {
        var (servico, vinculos, leitor, _, relogio, _) = Criar();
        leitor.Proxima = new AmostraDeJanela("Google Chrome", ReuniaoDaStraumann, false, "https://meet.google.com/abc-defg-hij");
        servico.Iniciar();
        Avancar(servico, relogio, 2);

        var alvo = servico.SegurarJanelaDaFrente(Janelinha)!;
        var formulario = vinculos.Formulario(alvo);
        var contagem = vinculos.FormularioDaContagem(alvo.Programa, alvo.Titulo);
        servico.SoltarJanelaDaFrente();

        Assert.Equal(new EscritorioIdentificado("1759", FonteDoEscritorio.Reuniao), alvo.Identificado);
        Assert.Equal("1759", formulario.Escritorio);
        Assert.Equal(alvo.Identificado, formulario.Identificado);
        Assert.Null(formulario.EmEdicao);
        Assert.True(contagem.Reuniao);
        Assert.Equal(ModoDeContagem.EnquantoAberta, contagem.Modo);
        Assert.Empty(vinculos.ListarVinculos());
    }

    [Fact]
    public void Atalho_em_reuniao_cujo_titulo_nao_identifica_escritorio_abre_em_branco()
    {
        var (servico, vinculos, leitor, _, relogio, _) = Criar();
        leitor.Proxima = new AmostraDeJanela("Google Chrome", "Meet: Acme <> SplitC - Google Chrome", false);
        servico.Iniciar();
        Avancar(servico, relogio, 1);

        var alvo = servico.SegurarJanelaDaFrente(Janelinha)!;
        var formulario = vinculos.Formulario(alvo);
        servico.SoltarJanelaDaFrente();

        Assert.Null(alvo.Identificado);
        Assert.False(alvo.Descoberto);
        Assert.Equal("", formulario.Escritorio);
        Assert.Null(formulario.Identificado);
    }

    [Fact]
    public void Atalho_no_chatwoot_abre_com_o_escritorio_do_chat_aberto()
    {
        var (servico, vinculos, leitor, _, relogio, _) = Criar();
        leitor.Proxima = new AmostraDeJanela(
            "Google Chrome",
            "Chatwoot - Google Chrome",
            false,
            "https://ticketing.splitc.com.br/app/accounts/1/inbox-view/conversation/22736",
            "SplitC <> Galena V2");
        servico.Iniciar();
        Avancar(servico, relogio, 1);

        var alvo = servico.SegurarJanelaDaFrente(Janelinha)!;
        servico.SoltarJanelaDaFrente();

        Assert.Equal(new EscritorioIdentificado("3155", FonteDoEscritorio.Chatwoot), alvo.Identificado);
        Assert.Equal("3155", vinculos.Formulario(alvo).Escritorio);
    }

    [Fact]
    public void Salvar_so_a_contagem_na_reuniao_identificada_nao_cria_vinculo()
    {
        var (servico, vinculos, leitor, _, relogio, _) = Criar();
        leitor.Proxima = new AmostraDeJanela("Google Chrome", ReuniaoDaStraumann, false);
        servico.Iniciar();
        Avancar(servico, relogio, 1);
        var alvo = servico.SegurarJanelaDaFrente(Janelinha)!;
        var formulario = vinculos.Formulario(alvo);

        var repete = formulario.SoRepeteOIdentificado(
            formulario.Escritorio,
            "",
            formulario.Alcance,
            formulario.SoRegistro,
            formulario.Validade);
        var mudou = vinculos.SalvarContagem(alvo.Programa, alvo.Titulo, AlcanceDaContagem.Janela, ModoDeContagem.SoEmFoco);
        servico.SoltarJanelaDaFrente();

        Assert.True(repete);
        Assert.True(mudou);
        Assert.Empty(vinculos.ListarVinculos());
        Assert.Equal(ModoDeContagem.SoEmFoco, vinculos.FormularioDaContagem(alvo.Programa, alvo.Titulo).Modo);
        Assert.Equal("Straumann", Assert.Single(ComTempo(servico)).Rotulo);
    }

    [Fact]
    public void Vinculo_com_outro_escritorio_nao_muda_o_registro_identificado_e_a_janelinha_segue_mostrando_o_identificado()
    {
        var (servico, vinculos, leitor, _, relogio, _) = Criar();
        leitor.Proxima = new AmostraDeJanela("Google Chrome", ReuniaoDaStraumann, false);
        servico.Iniciar();
        Avancar(servico, relogio, 5);
        var alvo = servico.SegurarJanelaDaFrente(Janelinha)!;
        var formulario = vinculos.Formulario(alvo);

        var salvo = servico.SalvarVinculo(formulario.EmEdicao, new PedidoDeVinculo(
            "3155",
            null,
            formulario.Alcance,
            formulario.Alvo,
            formulario.Validade,
            formulario.Inicio,
            null));
        servico.SoltarJanelaDaFrente();
        Avancar(servico, relogio, 5);
        var deNovo = servico.SegurarJanelaDaFrente(Janelinha)!;
        servico.SoltarJanelaDaFrente();

        Assert.NotNull(salvo.Vinculo);
        var linha = Assert.Single(ComTempo(servico));
        Assert.Equal("Straumann", linha.Rotulo);
        Assert.Equal(TimeSpan.FromMinutes(10), linha.DuracaoReunioes);
        Assert.Null(deNovo.VinculoId);
        Assert.Equal("1759", vinculos.Formulario(deNovo).Escritorio);
        Assert.NotNull(vinculos.Formulario(deNovo).Identificado);
    }

    [Fact]
    public void Escritorio_que_veio_de_uma_regra_do_titulo_abre_marcado_ate_existir_vinculo()
    {
        var (servico, vinculos, leitor, _, relogio, regras) = Criar();
        regras.Regras = [new RegraDeEscritorio("Straumann", ["Contrato"])];
        leitor.Proxima = new AmostraDeJanela("Word", "Contrato - Word", false);
        servico.Iniciar();
        Avancar(servico, relogio, 1);

        var pelaRegra = servico.SegurarJanelaDaFrente(Janelinha)!;
        var antes = vinculos.Formulario(pelaRegra);
        servico.SalvarVinculo(antes.EmEdicao, new PedidoDeVinculo(
            "3155",
            null,
            antes.Alcance,
            antes.Alvo,
            antes.Validade,
            antes.Inicio,
            null));
        servico.SoltarJanelaDaFrente();
        Avancar(servico, relogio, 1);
        var comVinculo = servico.SegurarJanelaDaFrente(Janelinha)!;
        var depois = vinculos.Formulario(comVinculo);
        servico.SoltarJanelaDaFrente();

        Assert.Equal(new EscritorioIdentificado("1759", FonteDoEscritorio.Regra), pelaRegra.Identificado);
        Assert.False(pelaRegra.Descoberto);
        Assert.Equal("1759", antes.Escritorio);
        Assert.NotNull(comVinculo.VinculoId);
        Assert.Equal(comVinculo.VinculoId, depois.EmEdicao);
        Assert.Equal("3155", depois.Escritorio);
        Assert.Null(depois.Identificado);
    }

    [Fact]
    public void Criar_outro_vinculo_continua_abrindo_em_branco()
    {
        var (servico, vinculos, leitor, _, relogio, regras) = Criar();
        regras.Regras = [new RegraDeEscritorio("Straumann", ["Contrato"])];
        leitor.Proxima = new AmostraDeJanela("Word", "Contrato - Word", false);
        servico.Iniciar();
        Avancar(servico, relogio, 1);

        var alvo = servico.SegurarJanelaDaFrente(Janelinha)!;
        var emBranco = vinculos.FormularioNovo(alvo);
        servico.SoltarJanelaDaFrente();

        Assert.Equal("", emBranco.Escritorio);
        Assert.Null(emBranco.Identificado);
    }

    private static IReadOnlyList<LinhaDeEscritorio> ComTempo(ServicoDeEsforco servico) =>
        servico.ConsultarEscritorios().Where(linha => linha.DuracaoContada > TimeSpan.Zero).ToList();

    private static void Avancar(ServicoDeEsforco servico, RelogioManual relogio, int minutos)
    {
        for (var i = 0; i < minutos * 6; i++)
        {
            relogio.Instante = relogio.Instante.AddSeconds(10);
            servico.Pulso();
        }
    }

    private static (ServicoDeEsforco Servico, ServicoDeVinculos Vinculos, LeitorFalso Leitor, RepositorioEmMemoria Repositorio, RelogioManual Relogio, RegrasFixas Regras) Criar()
    {
        var leitor = new LeitorFalso();
        var repositorio = new RepositorioEmMemoria();
        repositorio.DefinirNomeDeEscritorio("1759", "Straumann");
        repositorio.AdicionarVariacao("1759", "Straumann LATAM");
        repositorio.DefinirNomeDeEscritorio("3155", "Galena V2");
        var relogio = new RelogioManual
        {
            Instante = new DateTimeOffset(2026, 11, 17, 9, 0, 0, TimeSpan.FromHours(-3)),
        };
        var regras = new RegrasFixas();
        var vinculos = new ServicoDeVinculos(new RepositorioDeVinculosEmMemoria(), relogio);
        var servico = new ServicoDeEsforco(leitor, repositorio, regras, relogio, vinculos);
        return (servico, vinculos, leitor, repositorio, relogio, regras);
    }
}
