using Esforco.Aplicacao;
using Esforco.Dominio;
using Esforco.Testes.Apoio;

namespace Esforco.Testes.Aplicacao;

public class ContagemEnquantoAbertaTestes
{
    private const string Reuniao = "Meet: vct <> splitc - Google Chrome";
    private static readonly DateTime Hoje = new(2026, 11, 17);
    private static readonly AmostraDeJanela Discord = new("Discord", "amigos - Discord", false);
    private static readonly AmostraDeJanela Word = new("Word", "notas.docx - Word", false);
    private static readonly JanelaAberta DiscordAberto = new("Discord", "amigos - Discord", false);
    private static readonly JanelaAberta WordAberto = new("Word", "notas.docx - Word", false);

    [Fact]
    public void Sem_regra_nada_muda_e_as_janelas_abertas_nem_sao_listadas()
    {
        var (servico, _, leitor, repositorio, relogio) = Criar();
        leitor.Abertas.AddRange([DiscordAberto, WordAberto]);
        leitor.Proxima = Discord;
        servico.Iniciar();
        Passar(servico, relogio, 1);
        leitor.Proxima = Word;
        Passar(servico, relogio, 2);

        Assert.Equal(0, leitor.LeiturasDeAbertas);
        var lista = repositorio.Listar();
        Assert.Equal(2, lista.Count);
        Assert.Equal(TimeSpan.FromSeconds(65), lista[0].DuracaoAte(relogio.Instante));
        Assert.NotNull(lista[0].Fim);
        Assert.Empty(repositorio.ListarEmSegundoPlano());
    }

    [Fact]
    public void Janela_que_conta_aberta_segue_sem_foco_e_volta_para_a_frente_no_mesmo_registro()
    {
        var (servico, vinculos, leitor, repositorio, relogio) = Criar();
        Assert.True(vinculos.SalvarContagem("Discord", "amigos - Discord", AlcanceDaContagem.Janela, ModoDeContagem.EnquantoAberta));
        leitor.Abertas.AddRange([DiscordAberto, WordAberto]);
        leitor.Proxima = Discord;
        servico.Iniciar();
        var id = Assert.Single(repositorio.Listar()).Id;
        Passar(servico, relogio, 1);

        leitor.Proxima = Word;
        Passar(servico, relogio, 2);

        var fundo = Assert.Single(repositorio.ListarEmSegundoPlano());
        Assert.Equal(id, fundo.Id);
        Assert.Null(fundo.Fim);
        Assert.Equal(2, repositorio.Listar().Count);

        leitor.Abertas[0] = DiscordAberto with { Minimizada = true };
        Passar(servico, relogio, 1);
        Assert.Equal(id, Assert.Single(repositorio.ListarEmSegundoPlano()).Id);

        leitor.Abertas[0] = DiscordAberto;
        leitor.Proxima = Discord;
        Passar(servico, relogio, 1);

        Assert.Empty(repositorio.ListarEmSegundoPlano());
        Assert.Equal(id, repositorio.ObterAberto()!.Id);
        var lista = repositorio.Listar();
        Assert.Equal(2, lista.Count);
        Assert.Equal(TimeSpan.FromMinutes(5), lista[0].DuracaoAte(relogio.Instante));
        Assert.Equal(TimeSpan.FromMinutes(3), lista[1].DuracaoAte(relogio.Instante));

        var dia = Assert.Single(servico.ConsultarResumo(Hoje, Hoje).Nos);
        Assert.Equal(TimeSpan.FromMinutes(5), Assert.Single(dia.Filhos, no => no.Rotulo == "Discord").DuracaoTotal);
        Assert.Equal(TimeSpan.FromMinutes(3), Assert.Single(dia.Filhos, no => no.Rotulo == "Word").DuracaoTotal);
    }

    [Fact]
    public void Janela_fechada_encerra_no_ultimo_instante_em_que_foi_vista()
    {
        var (servico, vinculos, leitor, repositorio, relogio) = Criar();
        vinculos.SalvarContagem("Discord", "amigos - Discord", AlcanceDaContagem.Janela, ModoDeContagem.EnquantoAberta);
        leitor.Abertas.AddRange([DiscordAberto, WordAberto]);
        leitor.Proxima = Discord;
        servico.Iniciar();
        Passar(servico, relogio, 1);
        leitor.Proxima = Word;
        Passar(servico, relogio, 2);
        var vista = relogio.Instante;

        leitor.Abertas.RemoveAt(0);
        Passar(servico, relogio, 3);

        Assert.Empty(repositorio.ListarEmSegundoPlano());
        var discord = Assert.Single(repositorio.Listar(), item => item.Programa == "Discord");
        Assert.Equal(vista, discord.Fim);
        Assert.Equal(TimeSpan.FromMinutes(3), discord.DuracaoAte(relogio.Instante));
    }

    [Fact]
    public void Fechar_a_janela_que_estava_na_frente_encerra_no_ultimo_pulso_em_que_ela_apareceu()
    {
        var (servico, vinculos, leitor, repositorio, relogio) = Criar();
        vinculos.SalvarContagem("Discord", "amigos - Discord", AlcanceDaContagem.Janela, ModoDeContagem.EnquantoAberta);
        leitor.Abertas.AddRange([DiscordAberto, WordAberto]);
        leitor.Proxima = Discord;
        servico.Iniciar();
        Passar(servico, relogio, 1);
        var vista = relogio.Instante;

        leitor.Abertas.RemoveAt(0);
        leitor.Proxima = Word;
        Passar(servico, relogio, 1);

        var discord = Assert.Single(repositorio.Listar(), item => item.Programa == "Discord");
        Assert.Equal(vista, discord.Fim);
        Assert.Empty(repositorio.ListarEmSegundoPlano());
    }

    [Fact]
    public void Aba_do_navegador_atras_de_outra_aba_segue_pela_folga_da_reuniao()
    {
        var (servico, vinculos, leitor, repositorio, relogio) = Criar();
        var gather = "splitc-metaverse | Gather - Google Chrome";
        vinculos.SalvarContagem("Google Chrome", gather, AlcanceDaContagem.Janela, ModoDeContagem.EnquantoAberta);
        leitor.Abertas.Add(new JanelaAberta("Google Chrome", gather, false, Navegador: true));
        leitor.Proxima = new AmostraDeJanela("Google Chrome", gather, false);
        servico.Iniciar();
        Passar(servico, relogio, 1);
        var vista = relogio.Instante;

        leitor.Abertas[0] = new JanelaAberta("Google Chrome", "Gmail - Google Chrome", false, Navegador: true);
        leitor.Proxima = new AmostraDeJanela("Google Chrome", "Gmail - Google Chrome", false);
        Passar(servico, relogio, 30);

        var fundo = Assert.Single(repositorio.ListarEmSegundoPlano());
        Assert.Equal(gather, fundo.Titulo);
        Assert.Null(fundo.Fim);

        Passar(servico, relogio, 31);

        Assert.Empty(repositorio.ListarEmSegundoPlano());
        Assert.Equal(vista, Assert.Single(repositorio.Listar(), item => item.Titulo == gather).Fim);
    }

    [Fact]
    public void Aba_do_painel_sem_foco_e_reconhecida_pelo_titulo_da_janela_do_navegador()
    {
        var (servico, vinculos, leitor, repositorio, relogio) = Criar();
        var painel = "SplitC - Google Chrome";
        var endereco = "https://app.splitc.com.br/planos/17927/tab/862247?officeId=2200&periodId=49797&tabSelector=source&pagina=0&coluna=bq4&linha=19&rowId=19";
        leitor.Abertas.AddRange([new JanelaAberta("Google Chrome", painel, false, Navegador: true), WordAberto]);
        leitor.Proxima = new AmostraDeJanela("Google Chrome", painel, false, endereco);
        servico.Iniciar();
        var aberto = Assert.Single(repositorio.Listar());
        Assert.Equal("Painel SplitC", aberto.Programa);
        vinculos.SalvarContagem(aberto.Programa, aberto.Titulo, AlcanceDaContagem.Janela, ModoDeContagem.EnquantoAberta);
        Passar(servico, relogio, 1);

        // A aba segue à mostra em outra janela do Chrome: é vista a cada pulso, sem depender da folga.
        leitor.Proxima = Word;
        Passar(servico, relogio, 90);

        var fundo = Assert.Single(repositorio.ListarEmSegundoPlano());
        Assert.Equal(aberto.Id, fundo.Id);
        Assert.Null(fundo.Fim);
        var vista = relogio.Instante;

        // Outra aba passou na frente dela: vale a folga da reunião, e depois o registro encerra onde foi visto.
        leitor.Abertas[0] = new JanelaAberta("Google Chrome", "Gmail - Google Chrome", false, Navegador: true);
        Passar(servico, relogio, 30);
        Assert.Null(Assert.Single(repositorio.ListarEmSegundoPlano()).Fim);
        Passar(servico, relogio, 31);

        Assert.Empty(repositorio.ListarEmSegundoPlano());
        Assert.Equal(vista, Assert.Single(repositorio.Listar(), item => item.Programa == "Painel SplitC").Fim);
    }

    [Fact]
    public void Navegador_fechado_encerra_a_aba_sem_esperar_a_folga()
    {
        var (servico, vinculos, leitor, repositorio, relogio) = Criar();
        var gather = "splitc-metaverse | Gather - Google Chrome";
        vinculos.SalvarContagem("Google Chrome", gather, AlcanceDaContagem.Janela, ModoDeContagem.EnquantoAberta);
        leitor.Abertas.AddRange([new JanelaAberta("Google Chrome", gather, false, Navegador: true), WordAberto]);
        leitor.Proxima = Word;
        servico.Iniciar();
        Passar(servico, relogio, 2);
        var vista = relogio.Instante;

        leitor.Abertas.RemoveAt(0);
        Passar(servico, relogio, 1);

        Assert.Empty(repositorio.ListarEmSegundoPlano());
        Assert.Equal(vista, Assert.Single(repositorio.Listar(), item => item.Titulo == gather).Fim);
    }

    [Fact]
    public void Programa_inteiro_conta_enquanto_tiver_janela_aberta_mesmo_sem_nunca_ter_tido_foco()
    {
        var (servico, vinculos, leitor, repositorio, relogio) = Criar();
        vinculos.SalvarContagem("Discord", "amigos - Discord", AlcanceDaContagem.Programa, ModoDeContagem.EnquantoAberta);
        leitor.Abertas.AddRange([WordAberto, DiscordAberto with { Minimizada = true }]);
        leitor.Proxima = Word;
        servico.Iniciar();
        Passar(servico, relogio, 2);

        var fundo = Assert.Single(repositorio.ListarEmSegundoPlano());
        Assert.Equal("Discord", fundo.Programa);
        Assert.Equal("amigos - Discord", fundo.Titulo);
        Assert.True(fundo.Minimizada);
        Assert.Equal(TimeSpan.FromMinutes(2), fundo.DuracaoAte(relogio.Instante));
        Assert.Equal(2, repositorio.Listar().Count);

        leitor.Abertas[1] = new JanelaAberta("Discord", "trabalho - Discord", true);
        Passar(servico, relogio, 1);

        Assert.Equal(fundo.Id, Assert.Single(repositorio.ListarEmSegundoPlano()).Id);
        Assert.Equal(2, repositorio.Listar().Count);
    }

    [Fact]
    public void Outra_tela_do_mesmo_programa_na_frente_troca_o_registro_sem_contar_dobrado()
    {
        var (servico, vinculos, leitor, repositorio, relogio) = Criar();
        vinculos.SalvarContagem("Discord", "amigos - Discord", AlcanceDaContagem.Programa, ModoDeContagem.EnquantoAberta);
        leitor.Abertas.AddRange([DiscordAberto, WordAberto]);
        leitor.Proxima = Discord;
        servico.Iniciar();
        Passar(servico, relogio, 1);
        leitor.Proxima = Word;
        Passar(servico, relogio, 1);

        leitor.Abertas[0] = new JanelaAberta("Discord", "trabalho - Discord", false);
        leitor.Proxima = new AmostraDeJanela("Discord", "trabalho - Discord", false);
        Passar(servico, relogio, 1);

        Assert.Empty(repositorio.ListarEmSegundoPlano());
        var discord = repositorio.Listar().Where(item => item.Programa == "Discord").ToList();
        Assert.Equal(2, discord.Count);
        Assert.NotNull(discord[0].Fim);
        Assert.Equal(discord[0].Fim, discord[1].Inicio);
        Assert.Null(discord[1].Fim);
        var dia = Assert.Single(servico.ConsultarResumo(Hoje, Hoje).Nos);
        Assert.Equal(TimeSpan.FromMinutes(3), Assert.Single(dia.Filhos, no => no.Rotulo == "Discord").DuracaoTotal);
    }

    [Fact]
    public void Regra_da_janela_so_em_foco_tira_essa_janela_da_contagem_do_programa()
    {
        var (servico, vinculos, leitor, repositorio, relogio) = Criar();
        vinculos.SalvarContagem("Discord", "amigos - Discord", AlcanceDaContagem.Programa, ModoDeContagem.EnquantoAberta);
        Assert.True(vinculos.SalvarContagem("Discord", "amigos - Discord", AlcanceDaContagem.Janela, ModoDeContagem.SoEmFoco));
        leitor.Abertas.AddRange([DiscordAberto, WordAberto]);
        leitor.Proxima = Discord;
        servico.Iniciar();
        Passar(servico, relogio, 1);
        leitor.Proxima = Word;
        Passar(servico, relogio, 1);

        Assert.Empty(repositorio.ListarEmSegundoPlano());
        Assert.NotNull(Assert.Single(repositorio.Listar(), item => item.Programa == "Discord").Fim);
    }

    [Fact]
    public void Parar_bloquear_e_buraco_no_relogio_fecham_o_que_contava_aberto_como_a_reuniao()
    {
        var (servico, vinculos, leitor, repositorio, relogio) = Criar();
        vinculos.SalvarContagem("Discord", "amigos - Discord", AlcanceDaContagem.Janela, ModoDeContagem.EnquantoAberta);
        leitor.Abertas.AddRange([DiscordAberto, WordAberto]);
        leitor.Proxima = Word;
        servico.Iniciar();
        Passar(servico, relogio, 1);

        var bloqueio = relogio.Instante;
        servico.Ausentar();
        Assert.Empty(repositorio.ListarEmSegundoPlano());
        Assert.All(repositorio.Listar(), item => Assert.Equal(bloqueio, item.Fim));

        relogio.Instante = relogio.Instante.AddMinutes(10);
        servico.Retomar();
        servico.Pulso();
        Passar(servico, relogio, 1);
        var retomado = Assert.Single(repositorio.ListarEmSegundoPlano());
        Assert.Equal(bloqueio.AddMinutes(10), retomado.Inicio);

        var corte = relogio.Instante;
        relogio.Instante = corte.Add(ServicoDeEsforco.FolgaDeAusencia).AddSeconds(5);
        servico.Pulso();
        Assert.Equal(corte, Assert.Single(repositorio.Listar(), item => item.Id == retomado.Id).Fim);
        Assert.Equal(relogio.Instante, Assert.Single(repositorio.ListarEmSegundoPlano()).Inicio);

        Passar(servico, relogio, 1);
        var parada = relogio.Instante;
        servico.Parar();
        Assert.Empty(repositorio.ListarEmSegundoPlano());
        Assert.All(repositorio.Listar(), item => Assert.NotNull(item.Fim));

        var discord = repositorio.Listar().Where(item => item.Programa == "Discord").ToList();
        Assert.Equal(3, discord.Count);
        Assert.Equal(parada, discord[2].Fim);
        var dia = Assert.Single(servico.ConsultarResumo(Hoje, Hoje).Nos);
        Assert.Equal(TimeSpan.FromMinutes(3), Assert.Single(dia.Filhos, no => no.Rotulo == "Discord").DuracaoTotal);
    }

    [Fact]
    public void Trocar_para_so_em_foco_encerra_na_hora_o_que_contava_sem_foco()
    {
        var (servico, vinculos, leitor, repositorio, relogio) = Criar();
        vinculos.SalvarContagem("Discord", "amigos - Discord", AlcanceDaContagem.Janela, ModoDeContagem.EnquantoAberta);
        leitor.Abertas.AddRange([DiscordAberto, WordAberto]);
        leitor.Proxima = Word;
        servico.Iniciar();
        Passar(servico, relogio, 2);
        Assert.Single(repositorio.ListarEmSegundoPlano());

        Assert.True(vinculos.SalvarContagem("Discord", "amigos - Discord", AlcanceDaContagem.Janela, ModoDeContagem.SoEmFoco));
        relogio.Instante = relogio.Instante.AddSeconds(5);
        servico.Pulso();
        var troca = relogio.Instante;
        Passar(servico, relogio, 1);

        Assert.Empty(repositorio.ListarEmSegundoPlano());
        Assert.Equal(troca, Assert.Single(repositorio.Listar(), item => item.Programa == "Discord").Fim);
    }

    [Fact]
    public void Programa_pausado_nao_conta_aberto_e_nao_derruba_o_que_conta_atras_dele()
    {
        var (servico, vinculos, leitor, repositorio, relogio) = Criar();
        vinculos.SalvarContagem("Discord", "amigos - Discord", AlcanceDaContagem.Janela, ModoDeContagem.EnquantoAberta);
        vinculos.SalvarContagem("Spotify", "", AlcanceDaContagem.Programa, ModoDeContagem.EnquantoAberta);
        servico.AlternarPausado("Spotify");
        servico.AlternarPausado("Esforço");
        leitor.Abertas.AddRange([DiscordAberto, new JanelaAberta("Spotify", "Spotify Premium", false)]);
        leitor.Proxima = Discord;
        servico.Iniciar();
        Passar(servico, relogio, 1);

        leitor.Proxima = new AmostraDeJanela("Esforço", "Esforço", false, Propria: true);
        Passar(servico, relogio, 2);

        var unico = Assert.Single(repositorio.Listar());
        Assert.Equal("Discord", unico.Programa);
        Assert.Null(unico.Fim);
        Assert.True(unico.EmSegundoPlano);
        Assert.Equal(TimeSpan.FromMinutes(3), unico.DuracaoAte(relogio.Instante));

        var pausa = relogio.Instante;
        servico.AlternarPausado("Discord");
        Passar(servico, relogio, 1);
        Assert.Equal(pausa, Assert.Single(repositorio.Listar()).Fim);
    }

    [Fact]
    public void Falha_ao_listar_as_janelas_nao_fecha_nem_abre_nada()
    {
        var (servico, vinculos, leitor, repositorio, relogio) = Criar();
        vinculos.SalvarContagem("Discord", "amigos - Discord", AlcanceDaContagem.Janela, ModoDeContagem.EnquantoAberta);
        leitor.Abertas.AddRange([DiscordAberto, WordAberto]);
        leitor.Proxima = Word;
        servico.Iniciar();
        Passar(servico, relogio, 1);

        leitor.Abertas.Clear();
        leitor.FalhaNasAbertas = new InvalidOperationException("sem lista");
        Passar(servico, relogio, 2);

        var fundo = Assert.Single(repositorio.ListarEmSegundoPlano());
        Assert.Null(fundo.Fim);
        Assert.Equal(2, repositorio.Listar().Count);
    }

    [Fact]
    public void Reuniao_e_janela_aberta_contam_juntas_e_o_total_passa_do_relogio()
    {
        var (servico, vinculos, leitor, repositorio, relogio) = Criar();
        vinculos.SalvarContagem("Discord", "amigos - Discord", AlcanceDaContagem.Janela, ModoDeContagem.EnquantoAberta);
        leitor.Abertas.AddRange([DiscordAberto, WordAberto, new JanelaAberta("Google Chrome", Reuniao, false, Navegador: true)]);
        leitor.Proxima = new AmostraDeJanela("Google Chrome", Reuniao, false);
        servico.Iniciar();
        Passar(servico, relogio, 1);
        leitor.Proxima = Discord;
        Passar(servico, relogio, 1);
        leitor.Proxima = Word;
        Passar(servico, relogio, 1);
        leitor.Proxima = new AmostraDeJanela("Google Chrome", Reuniao, false);
        Passar(servico, relogio, 1);

        var lista = repositorio.Listar();
        Assert.Equal(3, lista.Count);
        Assert.Null(Assert.Single(lista, item => item.Titulo == Reuniao).Fim);
        Assert.Null(Assert.Single(lista, item => item.Programa == "Discord").Fim);
        Assert.NotNull(Assert.Single(lista, item => item.Programa == "Word").Fim);
        Assert.Equal("Discord", Assert.Single(repositorio.ListarEmSegundoPlano()).Programa);
        Assert.Equal(Reuniao, repositorio.ObterAberto()!.Titulo);
        var dia = Assert.Single(servico.ConsultarResumo(Hoje, Hoje).Nos);
        Assert.Equal(TimeSpan.FromMinutes(4), Assert.Single(dia.Filhos, no => no.Rotulo == "Google Chrome").DuracaoTotal);
        Assert.Equal(TimeSpan.FromMinutes(4), Assert.Single(dia.Filhos, no => no.Rotulo == "Discord").DuracaoTotal);
        Assert.Equal(TimeSpan.FromMinutes(1), Assert.Single(dia.Filhos, no => no.Rotulo == "Word").DuracaoTotal);
    }

    [Fact]
    public void Reuniao_marcada_so_em_foco_para_ao_perder_o_foco_e_nao_emenda_o_tempo_do_meio()
    {
        var (servico, vinculos, leitor, repositorio, relogio) = Criar();
        Assert.True(vinculos.SalvarContagem("Google Chrome", Reuniao, AlcanceDaContagem.Janela, ModoDeContagem.SoEmFoco));
        leitor.Proxima = new AmostraDeJanela("Google Chrome", Reuniao, false);
        servico.Iniciar();
        Passar(servico, relogio, 1);

        leitor.Proxima = Word;
        leitor.ReuniaoMinimizada = new AmostraDeJanela("Google Chrome", Reuniao, true);
        Passar(servico, relogio, 2);

        Assert.Empty(repositorio.ListarEmSegundoPlano());
        Assert.NotNull(Assert.Single(repositorio.Listar(), item => item.Titulo == Reuniao).Fim);

        leitor.ReuniaoMinimizada = null;
        leitor.Proxima = new AmostraDeJanela("Google Chrome", "Meet: vct <> splitc", false);
        Passar(servico, relogio, 1);
        servico.Parar();

        Assert.Equal(2, repositorio.Listar().Count(item => item.Programa == "Google Chrome"));
        servico.DefinirNomeDoEscritorio("1905", "VCT");
        var linha = Assert.Single(servico.ConsultarEscritorios(Hoje, Hoje));
        Assert.Equal(TimeSpan.FromMinutes(2), linha.DuracaoReunioes);
        var dia = Assert.Single(servico.ConsultarResumo(Hoje, Hoje).Nos);
        Assert.Equal(TimeSpan.FromMinutes(2), Assert.Single(dia.Filhos, no => no.Rotulo == "Google Chrome").DuracaoTotal);
    }

    [Fact]
    public void Reuniao_volta_a_contar_aberta_quando_a_escolha_e_desfeita()
    {
        var (servico, vinculos, leitor, repositorio, relogio) = Criar();
        vinculos.SalvarContagem("Google Chrome", Reuniao, AlcanceDaContagem.Janela, ModoDeContagem.SoEmFoco);
        Assert.True(vinculos.SalvarContagem("Google Chrome", Reuniao, AlcanceDaContagem.Janela, ModoDeContagem.EnquantoAberta));
        leitor.Proxima = new AmostraDeJanela("Google Chrome", Reuniao, false);
        servico.Iniciar();
        Passar(servico, relogio, 1);
        leitor.Proxima = Word;
        Passar(servico, relogio, 2);

        var reuniao = Assert.Single(repositorio.ListarEmSegundoPlano());
        Assert.Equal(Reuniao, reuniao.Titulo);
        Assert.Equal(TimeSpan.FromMinutes(3), reuniao.DuracaoAte(relogio.Instante));
    }

    [Fact]
    public void Contagem_e_salva_sem_escritorio_e_reaparece_ao_abrir_a_janelinha_de_novo()
    {
        var (servico, vinculos, leitor, _, _) = Criar();
        leitor.Proxima = Discord;
        servico.Iniciar();
        var alvo = servico.SegurarJanelaDaFrente("Vincular a um escritório")!;
        var antes = vinculos.FormularioDaContagem(alvo.Programa, alvo.Titulo);

        var mudou = vinculos.SalvarContagem(alvo.Programa, alvo.Titulo, AlcanceDaContagem.Janela, ModoDeContagem.EnquantoAberta);
        servico.SoltarJanelaDaFrente();
        var depois = vinculos.FormularioDaContagem(alvo.Programa, alvo.Titulo);

        Assert.False(antes.Salva);
        Assert.Equal(ModoDeContagem.SoEmFoco, antes.Modo);
        Assert.True(mudou);
        Assert.True(depois.Salva);
        Assert.Equal(ModoDeContagem.EnquantoAberta, depois.Modo);
        Assert.Equal(AlcanceDaContagem.Janela, depois.Alcance);
        Assert.Empty(vinculos.ListarVinculos());
        Assert.False(vinculos.SalvarContagem(alvo.Programa, alvo.Titulo, AlcanceDaContagem.Janela, ModoDeContagem.EnquantoAberta));
    }

    private static void Passar(ServicoDeEsforco servico, RelogioManual relogio, int minutos)
    {
        for (var i = 0; i < minutos * 12; i++)
        {
            relogio.Instante = relogio.Instante.AddSeconds(5);
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
