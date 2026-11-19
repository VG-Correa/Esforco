using Esforco.Aplicacao;
using Esforco.Dominio;
using Esforco.Testes.Apoio;

namespace Esforco.Testes.Aplicacao;

/// <summary>
/// O que conta enquanto aberto para de contar quando a aba, a janela ou a página é fechada,
/// e o registro termina onde foi visto pela última vez.
/// </summary>
public class AbaFechadaParaDeContarTestes
{
    private const string Reuniao = "Meet: Straumann LATAM <> SplitC - Google Chrome";
    private const string AbaDaReuniao = "Uso da memória em Meet: Straumann LATAM <> SplitC: parte do grupo SplitC: 412 MB";
    private const string Gmail = "Gmail - Google Chrome";
    private const string AbaDoGmail = "Uso da memória em Gmail: 300 MB";
    private const string Relatorio = "Serasa <> SplitC - Dashboard | Relatório da reunião | Triple Session - Google Chrome";
    private static readonly AmostraDeJanela Word = new("Word", "notas.docx - Word", false);
    private static readonly JanelaAberta WordAberto = new("Word", "notas.docx - Word", false);

    [Fact]
    public void Reuniao_atras_de_outra_aba_segue_contando_e_para_quando_a_aba_e_fechada()
    {
        var (servico, _, leitor, repositorio, relogio) = Criar();
        leitor.Abertas.AddRange([Chrome(Reuniao, AbaDaReuniao, AbaDoGmail), WordAberto]);
        leitor.Proxima = Frente(Reuniao);
        servico.Iniciar();
        var id = Assert.Single(repositorio.Listar()).Id;
        Passar(servico, relogio, 60);

        // Outra aba da mesma janela passa na frente, e a pessoa trabalha em outro programa por mais de uma hora.
        leitor.Abertas[0] = Chrome(Gmail, AbaDaReuniao, AbaDoGmail);
        leitor.Proxima = Word;
        Passar(servico, relogio, 70 * 60);

        var fundo = Assert.Single(repositorio.ListarEmSegundoPlano());
        Assert.Equal(id, fundo.Id);
        Assert.Null(fundo.Fim);
        var vista = relogio.Instante;

        leitor.Abertas[0] = Chrome(Gmail, AbaDoGmail);
        Passar(servico, relogio, 60);

        Assert.Empty(repositorio.ListarEmSegundoPlano());
        var reuniao = Assert.Single(repositorio.Listar(), item => item.Id == id);
        Assert.Equal(vista, reuniao.Fim);
        Assert.Equal(TimeSpan.FromMinutes(71), reuniao.DuracaoAte(relogio.Instante));
    }

    [Fact]
    public void Fechar_a_aba_da_reuniao_que_estava_na_frente_encerra_onde_ela_foi_vista()
    {
        var (servico, _, leitor, repositorio, relogio) = Criar();
        leitor.Abertas.Add(Chrome(Reuniao, AbaDaReuniao, AbaDoGmail));
        leitor.Proxima = Frente(Reuniao);
        servico.Iniciar();
        Passar(servico, relogio, 120);
        var vista = relogio.Instante;

        leitor.Abertas[0] = Chrome(Gmail, AbaDoGmail);
        leitor.Proxima = Frente(Gmail);
        Passar(servico, relogio, 20);
        Assert.Null(Assert.Single(repositorio.ListarEmSegundoPlano()).Fim);

        Passar(servico, relogio, 40);

        Assert.Empty(repositorio.ListarEmSegundoPlano());
        Assert.Equal(vista, Assert.Single(repositorio.Listar(), item => item.Titulo == Reuniao).Fim);
    }

    [Fact]
    public void Fechar_o_navegador_inteiro_encerra_a_reuniao_onde_ela_foi_vista()
    {
        var (servico, _, leitor, repositorio, relogio) = Criar();
        leitor.Abertas.AddRange([Chrome(Reuniao, AbaDaReuniao), WordAberto]);
        leitor.Proxima = Frente(Reuniao);
        servico.Iniciar();
        Passar(servico, relogio, 60);
        leitor.Proxima = Word;
        Passar(servico, relogio, 60);
        var vista = relogio.Instante;

        leitor.Abertas.RemoveAt(0);
        Passar(servico, relogio, 60);

        Assert.Empty(repositorio.ListarEmSegundoPlano());
        Assert.Equal(vista, Assert.Single(repositorio.Listar(), item => item.Titulo == Reuniao).Fim);
    }

    [Fact]
    public void Parar_logo_depois_de_fechar_a_aba_nao_conta_o_tempo_em_que_ela_ja_nao_existia()
    {
        var (servico, _, leitor, repositorio, relogio) = Criar();
        leitor.Abertas.AddRange([Chrome(Gmail, AbaDaReuniao, AbaDoGmail), WordAberto]);
        leitor.Proxima = Frente(Reuniao);
        servico.Iniciar();
        leitor.Proxima = Word;
        Passar(servico, relogio, 300);
        var vista = relogio.Instante;

        leitor.Abertas[0] = Chrome(Gmail, AbaDoGmail);
        Passar(servico, relogio, 15);
        Assert.Null(Assert.Single(repositorio.ListarEmSegundoPlano()).Fim);
        servico.Parar();

        Assert.Equal(vista, Assert.Single(repositorio.Listar(), item => item.Titulo == Reuniao).Fim);
    }

    [Fact]
    public void Bloquear_a_tela_logo_depois_de_fechar_a_aba_tambem_encerra_onde_ela_foi_vista()
    {
        var (servico, _, leitor, repositorio, relogio) = Criar();
        leitor.Abertas.AddRange([Chrome(Gmail, AbaDaReuniao, AbaDoGmail), WordAberto]);
        leitor.Proxima = Frente(Reuniao);
        servico.Iniciar();
        leitor.Proxima = Word;
        Passar(servico, relogio, 300);
        var vista = relogio.Instante;

        leitor.Abertas[0] = Chrome(Gmail, AbaDoGmail);
        Passar(servico, relogio, 15);
        servico.Ausentar();

        Assert.Equal(vista, Assert.Single(repositorio.Listar(), item => item.Titulo == Reuniao).Fim);
    }

    [Fact]
    public void Parar_com_a_reuniao_aberta_atras_de_outra_aba_conta_ate_a_parada()
    {
        var (servico, _, leitor, repositorio, relogio) = Criar();
        leitor.Abertas.AddRange([Chrome(Gmail, AbaDaReuniao, AbaDoGmail), WordAberto]);
        leitor.Proxima = Frente(Reuniao);
        servico.Iniciar();
        leitor.Proxima = Word;
        Passar(servico, relogio, 300);
        var parada = relogio.Instante;
        servico.Parar();

        Assert.Equal(parada, Assert.Single(repositorio.Listar(), item => item.Titulo == Reuniao).Fim);
    }

    [Fact]
    public void Aba_que_some_por_instantes_e_volta_nao_encerra_a_reuniao()
    {
        var (servico, _, leitor, repositorio, relogio) = Criar();
        leitor.Abertas.AddRange([Chrome(Gmail, AbaDaReuniao, AbaDoGmail), WordAberto]);
        leitor.Proxima = Frente(Reuniao);
        servico.Iniciar();
        var id = Assert.Single(repositorio.Listar()).Id;
        leitor.Proxima = Word;
        Passar(servico, relogio, 120);

        // A página recarrega: por alguns segundos a aba mostra o endereço no lugar do título.
        leitor.Abertas[0] = Chrome(Gmail, "meet.google.com/mgs-jeuy-unz", AbaDoGmail);
        Passar(servico, relogio, 20);
        leitor.Abertas[0] = Chrome(Gmail, AbaDaReuniao, AbaDoGmail);
        Passar(servico, relogio, 120);

        var fundo = Assert.Single(repositorio.ListarEmSegundoPlano());
        Assert.Equal(id, fundo.Id);
        Assert.Null(fundo.Fim);
        Assert.Equal(2, repositorio.Listar().Count);
    }

    [Fact]
    public void Lista_de_abas_incompleta_nao_encerra_a_reuniao_antes_da_folga()
    {
        var (servico, _, leitor, repositorio, relogio) = Criar();
        leitor.Abertas.AddRange([Chrome(Gmail, "Agenda"), WordAberto]);
        leitor.Proxima = Frente(Reuniao);
        servico.Iniciar();
        var vista = relogio.Instante;
        leitor.Proxima = Word;
        Passar(servico, relogio, 50 * 60);

        Assert.Null(Assert.Single(repositorio.ListarEmSegundoPlano()).Fim);

        Passar(servico, relogio, 11 * 60);

        Assert.Empty(repositorio.ListarEmSegundoPlano());
        Assert.Equal(vista, Assert.Single(repositorio.Listar(), item => item.Titulo == Reuniao).Fim);
    }

    [Fact]
    public void Grupo_de_abas_recolhido_nao_deixa_dar_a_reuniao_como_fechada_antes_da_folga()
    {
        var (servico, _, leitor, repositorio, relogio) = Criar();
        leitor.Abertas.AddRange([Chrome(Gmail, AbaDoGmail) with { PodeFaltarAba = true }, WordAberto]);
        leitor.Proxima = Frente(Reuniao);
        servico.Iniciar();
        var vista = relogio.Instante;
        leitor.Proxima = Word;
        Passar(servico, relogio, 50 * 60);

        Assert.Null(Assert.Single(repositorio.ListarEmSegundoPlano()).Fim);

        Passar(servico, relogio, 11 * 60);

        Assert.Equal(vista, Assert.Single(repositorio.Listar(), item => item.Titulo == Reuniao).Fim);
    }

    [Fact]
    public void Uma_leitura_so_dizendo_fechada_nao_encerra_a_reuniao()
    {
        var (servico, _, leitor, repositorio, relogio) = Criar();
        leitor.Abertas.AddRange([Chrome(Gmail, AbaDaReuniao, AbaDoGmail), WordAberto]);
        leitor.Proxima = Frente(Reuniao);
        servico.Iniciar();
        leitor.Proxima = Word;
        Passar(servico, relogio, 120);

        leitor.Abertas[0] = Chrome(Gmail, AbaDoGmail);
        Passar(servico, relogio, 5);
        leitor.Abertas[0] = new JanelaAberta("Google Chrome", Gmail, false, Navegador: true);
        Passar(servico, relogio, 30 * 60);

        Assert.Null(Assert.Single(repositorio.ListarEmSegundoPlano()).Fim);
    }

    [Fact]
    public void Reuniao_a_mostra_em_outra_janela_sem_foco_e_sem_minimizar_segue_contando()
    {
        var (servico, _, leitor, repositorio, relogio) = Criar();
        leitor.Abertas.AddRange([new JanelaAberta("Google Chrome", Reuniao, false, Navegador: true), WordAberto]);
        leitor.Proxima = Frente(Reuniao);
        servico.Iniciar();
        leitor.Proxima = Word;
        Passar(servico, relogio, 70 * 60);

        Assert.Null(Assert.Single(repositorio.ListarEmSegundoPlano()).Fim);
        Assert.Equal(0, leitor.LeiturasDeAbas);

        var vista = relogio.Instante;
        leitor.Abertas.RemoveAt(0);
        Passar(servico, relogio, 60);

        Assert.Equal(vista, Assert.Single(repositorio.Listar(), item => item.Titulo == Reuniao).Fim);
    }

    [Fact]
    public void Abas_so_sao_lidas_quando_algo_conta_fora_do_titulo_das_janelas_e_no_maximo_a_cada_cinco_segundos()
    {
        var (servico, _, leitor, _, relogio) = Criar();
        leitor.Abertas.AddRange([Chrome(Reuniao, AbaDaReuniao, AbaDoGmail), WordAberto]);
        leitor.Proxima = Frente(Reuniao);
        servico.Iniciar();
        PassarDeSegundoEmSegundo(servico, relogio, 60);
        leitor.Proxima = Word;
        PassarDeSegundoEmSegundo(servico, relogio, 60);
        Assert.Equal(0, leitor.LeiturasDeAbas);

        leitor.Abertas[0] = Chrome(Gmail, AbaDaReuniao, AbaDoGmail);
        PassarDeSegundoEmSegundo(servico, relogio, 60);

        Assert.InRange(leitor.LeiturasDeAbas, 10, 12);
    }

    [Fact]
    public void Pagina_que_so_cita_a_reuniao_conta_so_em_foco()
    {
        var (servico, _, leitor, repositorio, relogio) = Criar();
        leitor.Abertas.AddRange([Chrome(Relatorio, "Serasa <> SplitC - Dashboard | Relatório da reunião | Triple Session"), WordAberto]);
        leitor.Proxima = Frente(Relatorio);
        servico.Iniciar();
        Passar(servico, relogio, 60);
        leitor.Proxima = Word;
        var saida = relogio.Instante.AddSeconds(5);
        Passar(servico, relogio, 300);

        Assert.Empty(repositorio.ListarEmSegundoPlano());
        var relatorio = Assert.Single(repositorio.Listar(), item => item.Titulo == Relatorio);
        Assert.Equal(saida, relatorio.Fim);
        Assert.Equal(TimeSpan.FromSeconds(65), relatorio.DuracaoAte(relogio.Instante));
    }

    [Fact]
    public void Pagina_que_so_cita_outra_reuniao_nao_encerra_a_reuniao_que_esta_aberta()
    {
        var (servico, _, leitor, repositorio, relogio) = Criar();
        var abaDoRelatorio = "Serasa <> SplitC - Dashboard | Relatório da reunião | Triple Session";
        leitor.Abertas.Add(Chrome(Reuniao, AbaDaReuniao, abaDoRelatorio));
        leitor.Proxima = Frente(Reuniao);
        servico.Iniciar();
        var id = Assert.Single(repositorio.Listar()).Id;
        Passar(servico, relogio, 60);

        leitor.Abertas[0] = Chrome(Relatorio, AbaDaReuniao, abaDoRelatorio);
        leitor.Proxima = Frente(Relatorio);
        Passar(servico, relogio, 120);

        var fundo = Assert.Single(repositorio.ListarEmSegundoPlano());
        Assert.Equal(id, fundo.Id);
        Assert.Null(fundo.Fim);
        Assert.Equal(Relatorio, repositorio.ObterAberto()!.Titulo);

        leitor.Abertas[0] = Chrome(Reuniao, AbaDaReuniao, abaDoRelatorio);
        leitor.Proxima = Frente(Reuniao);
        Passar(servico, relogio, 60);

        Assert.Equal(id, repositorio.ObterAberto()!.Id);
        var lista = repositorio.Listar();
        Assert.Equal(2, lista.Count);
        Assert.Equal(TimeSpan.FromMinutes(4), lista.Single(item => item.Id == id).DuracaoAte(relogio.Instante));
        Assert.Equal(TimeSpan.FromMinutes(2), lista.Single(item => item.Titulo == Relatorio).DuracaoAte(relogio.Instante));
    }

    [Fact]
    public void Outra_chamada_na_frente_encerra_a_reuniao_anterior_como_antes()
    {
        var (servico, _, leitor, repositorio, relogio) = Criar();
        var outra = "Meet: Serasa <> SplitC - Acompanhamento - Google Chrome";
        leitor.Abertas.Add(Chrome(Reuniao, AbaDaReuniao));
        leitor.Proxima = Frente(Reuniao);
        servico.Iniciar();
        Passar(servico, relogio, 60);
        leitor.Abertas[0] = Chrome(outra, AbaDaReuniao, "Meet: Serasa <> SplitC - Acompanhamento");
        leitor.Proxima = Frente(outra);
        var troca = relogio.Instante.AddSeconds(5);
        Passar(servico, relogio, 60);

        Assert.Equal(troca, Assert.Single(repositorio.Listar(), item => item.Titulo == Reuniao).Fim);
        Assert.Equal(outra, repositorio.ObterAberto()!.Titulo);
    }

    [Fact]
    public void Aba_com_regra_de_contar_aberta_segue_atras_de_outra_aba_e_para_quando_e_fechada()
    {
        var (servico, vinculos, leitor, repositorio, relogio) = Criar();
        var gather = "splitc-metaverse | Gather - Google Chrome";
        var abaDoGather = "Uso elevado da memória em splitc-metaverse | Gather: parte do grupo SplitC: 1,0 GB";
        vinculos.SalvarContagem("Google Chrome", gather, AlcanceDaContagem.Janela, ModoDeContagem.EnquantoAberta);
        leitor.Abertas.AddRange([Chrome(gather, abaDoGather, AbaDoGmail), WordAberto]);
        leitor.Proxima = Frente(gather);
        servico.Iniciar();
        Passar(servico, relogio, 60);

        leitor.Abertas[0] = Chrome(Gmail, abaDoGather, AbaDoGmail);
        leitor.Proxima = Word;
        Passar(servico, relogio, 70 * 60);

        Assert.Null(Assert.Single(repositorio.ListarEmSegundoPlano()).Fim);
        var vista = relogio.Instante;

        leitor.Abertas[0] = Chrome(Gmail, AbaDoGmail);
        Passar(servico, relogio, 60);

        Assert.Empty(repositorio.ListarEmSegundoPlano());
        var registro = Assert.Single(repositorio.Listar(), item => item.Titulo == gather);
        Assert.Equal(vista, registro.Fim);
        Assert.Equal(TimeSpan.FromMinutes(71), registro.DuracaoAte(relogio.Instante));
    }

    [Fact]
    public void Aba_do_chatwoot_com_regra_do_programa_inteiro_para_quando_a_ultima_aba_dele_e_fechada()
    {
        var (servico, vinculos, leitor, repositorio, relogio) = Criar();
        var chatwoot = "Chatwoot - Google Chrome";
        var abaDoChatwoot = "Uso da memória em Chatwoot: parte do grupo SplitC: 331 MB";
        leitor.Abertas.AddRange([Chrome(chatwoot, abaDoChatwoot, AbaDoGmail), WordAberto]);
        leitor.Proxima = new AmostraDeJanela(
            "Google Chrome",
            chatwoot,
            false,
            "https://ticketing.splitc.com.br/app/accounts/1/inbox-view/conversation/22736",
            "SplitC <> Concha y Toro - VCT");
        servico.Iniciar();
        var aberto = Assert.Single(repositorio.Listar());
        Assert.Equal("Chatwoot", aberto.Programa);
        vinculos.SalvarContagem(aberto.Programa, aberto.Titulo, AlcanceDaContagem.Programa, ModoDeContagem.EnquantoAberta);
        Passar(servico, relogio, 60);

        leitor.Abertas[0] = Chrome(Gmail, abaDoChatwoot, AbaDoGmail);
        leitor.Proxima = Word;
        Passar(servico, relogio, 70 * 60);

        Assert.Null(Assert.Single(repositorio.ListarEmSegundoPlano()).Fim);
        var vista = relogio.Instante;

        leitor.Abertas[0] = Chrome(Gmail, AbaDoGmail);
        Passar(servico, relogio, 60);

        Assert.Empty(repositorio.ListarEmSegundoPlano());
        Assert.Equal(vista, Assert.Single(repositorio.Listar(), item => item.Programa == "Chatwoot").Fim);
    }

    [Fact]
    public void Pausar_o_programa_logo_depois_de_fechar_a_aba_encerra_onde_ela_foi_vista()
    {
        var (servico, vinculos, leitor, repositorio, relogio) = Criar();
        var gather = "splitc-metaverse | Gather - Google Chrome";
        vinculos.SalvarContagem("Google Chrome", gather, AlcanceDaContagem.Janela, ModoDeContagem.EnquantoAberta);
        leitor.Abertas.AddRange([Chrome(Gmail, "splitc-metaverse | Gather", AbaDoGmail), WordAberto]);
        leitor.Proxima = Frente(gather);
        servico.Iniciar();
        leitor.Proxima = Word;
        Passar(servico, relogio, 300);
        var vista = relogio.Instante;

        leitor.Abertas[0] = Chrome(Gmail, AbaDoGmail);
        Passar(servico, relogio, 15);
        Assert.Null(Assert.Single(repositorio.ListarEmSegundoPlano()).Fim);
        servico.AlternarPausado("Google Chrome");

        Assert.Equal(vista, Assert.Single(repositorio.Listar(), item => item.Titulo == gather).Fim);
    }

    private static JanelaAberta Chrome(string titulo, params string[] abas) =>
        new("Google Chrome", titulo, false, Navegador: true, Abas: abas);

    private static AmostraDeJanela Frente(string titulo) => new("Google Chrome", titulo, false);

    private static void Passar(ServicoDeEsforco servico, RelogioManual relogio, int segundos)
    {
        for (var i = 0; i < segundos / 5; i++)
        {
            relogio.Instante = relogio.Instante.AddSeconds(5);
            servico.Pulso();
        }
    }

    private static void PassarDeSegundoEmSegundo(ServicoDeEsforco servico, RelogioManual relogio, int segundos)
    {
        for (var i = 0; i < segundos; i++)
        {
            relogio.Instante = relogio.Instante.AddSeconds(1);
            servico.Pulso();
        }
    }

    private static (ServicoDeEsforco Servico, ServicoDeVinculos Vinculos, LeitorFalso Leitor, RepositorioEmMemoria Repositorio, RelogioManual Relogio) Criar()
    {
        var leitor = new LeitorFalso();
        var repositorio = new RepositorioEmMemoria();
        var relogio = new RelogioManual
        {
            Instante = new DateTimeOffset(2026, 11, 17, 9, 0, 0, TimeSpan.FromHours(-3)),
        };
        var vinculos = new ServicoDeVinculos(new RepositorioDeVinculosEmMemoria(), relogio);
        var servico = new ServicoDeEsforco(leitor, repositorio, new RegrasFixas(), relogio, vinculos);
        return (servico, vinculos, leitor, repositorio, relogio);
    }
}
