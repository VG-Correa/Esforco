using Esforco.Aplicacao;
using Esforco.Dominio;
using Esforco.Testes.Apoio;

namespace Esforco.Testes.Aplicacao;

public class ServicoDeEsforcoTestes
{
    [Fact]
    public void Parado_nao_le_a_janela()
    {
        var (servico, leitor, _, _, _) = Cenario.Criar();

        servico.Pulso();
        servico.Pulso();

        Assert.Equal(0, leitor.Leituras);
        Assert.False(servico.EmLeitura);
    }

    [Fact]
    public void Iniciar_le_e_abre_intervalo()
    {
        var (servico, leitor, repositorio, _, _) = Cenario.Criar();
        leitor.Proxima = new AmostraDeJanela("Excel", "a.xlsx - Excel", false);

        servico.Iniciar();

        Assert.Equal(1, leitor.Leituras);
        Assert.True(servico.EmLeitura);
        var intervalo = Assert.Single(repositorio.Listar());
        Assert.Equal("Excel", intervalo.Programa);
        Assert.Equal("a.xlsx", intervalo.Contexto);
        Assert.Equal("", intervalo.Escritorio);
        Assert.Null(intervalo.Fim);
    }

    [Fact]
    public void Iniciar_de_novo_nao_le_outra_vez()
    {
        var (servico, leitor, _, _, _) = Cenario.Criar();
        leitor.Proxima = new AmostraDeJanela("Excel", "a.xlsx - Excel", false);

        servico.Iniciar();
        servico.Iniciar();

        Assert.Equal(1, leitor.Leituras);
    }

    [Fact]
    public void Mesma_janela_nao_abre_outro_intervalo()
    {
        var (servico, leitor, repositorio, relogio, _) = Cenario.Criar();
        leitor.Proxima = new AmostraDeJanela("Cursor", "MainWindow.xaml.cs - Esforco - Cursor", false);
        servico.Iniciar();

        relogio.Instante = relogio.Instante.AddSeconds(4);
        servico.Pulso();

        Assert.Equal(2, leitor.Leituras);
        Assert.Equal(1, repositorio.Vistos);
        var intervalo = Assert.Single(repositorio.Listar());
        Assert.Equal("MainWindow.xaml.cs", intervalo.Contexto);
        Assert.Null(intervalo.Fim);
    }

    [Fact]
    public void Troca_de_titulo_fecha_o_intervalo_e_abre_outro()
    {
        var (servico, leitor, repositorio, relogio, _) = Cenario.Criar();
        var inicio = relogio.Instante;
        leitor.Proxima = new AmostraDeJanela("Excel", "a.xlsx - Excel", false);
        servico.Iniciar();

        PassarTempo(servico, relogio, TimeSpan.FromMinutes(3));
        leitor.Proxima = new AmostraDeJanela("Excel", "b.xlsx - Excel", false);
        servico.Pulso();

        var lista = repositorio.Listar();
        Assert.Equal(2, lista.Count);
        Assert.Equal(inicio.AddMinutes(3), lista[0].Fim);
        Assert.Equal("a.xlsx", lista[0].Contexto);
        Assert.Null(lista[1].Fim);
        Assert.Equal("b.xlsx", lista[1].Contexto);
    }

    [Fact]
    public void Minimizar_fecha_o_intervalo_e_abre_outro()
    {
        var (servico, leitor, repositorio, _, _) = Cenario.Criar();
        leitor.Proxima = new AmostraDeJanela("Bloco de notas", "nota.txt - Bloco de notas", false);
        servico.Iniciar();

        leitor.Proxima = new AmostraDeJanela("Bloco de notas", "nota.txt - Bloco de notas", true);
        servico.Pulso();

        var lista = repositorio.Listar();
        Assert.Equal(2, lista.Count);
        Assert.False(lista[0].Minimizada);
        Assert.NotNull(lista[0].Fim);
        Assert.True(lista[1].Minimizada);
    }

    [Fact]
    public void Troca_de_programa_fecha_o_intervalo_e_abre_outro()
    {
        var (servico, leitor, repositorio, _, _) = Cenario.Criar();
        leitor.Proxima = new AmostraDeJanela("Word", "relatorio.docx - Word", false);
        servico.Iniciar();

        leitor.Proxima = new AmostraDeJanela("Chrome", "Caixa de entrada - Google Chrome", false);
        servico.Pulso();

        var lista = repositorio.Listar();
        Assert.Equal(["Word", "Chrome"], lista.Select(item => item.Programa));
        Assert.NotNull(lista[0].Fim);
        Assert.Null(lista[1].Fim);
    }

    [Fact]
    public void Parar_fecha_o_intervalo_e_deixa_de_ler()
    {
        var (servico, leitor, repositorio, relogio, _) = Cenario.Criar();
        leitor.Proxima = new AmostraDeJanela("Word", "relatorio.docx - Word", false);
        servico.Iniciar();
        relogio.Instante = relogio.Instante.AddMinutes(2);

        servico.Parar();
        servico.Pulso();

        Assert.False(servico.EmLeitura);
        Assert.Equal(1, leitor.Leituras);
        var intervalo = Assert.Single(repositorio.Listar());
        Assert.Equal(TimeSpan.FromMinutes(2), intervalo.DuracaoAte(relogio.Instante));
    }

    [Fact]
    public void Escritorio_fica_vazio_ate_a_regra_casar_o_titulo()
    {
        var (servico, leitor, repositorio, _, regras) = Cenario.Criar();
        regras.Regras = [new RegraDeEscritorio("Centro", ["planilha secreta"])];
        leitor.Proxima = new AmostraDeJanela("Excel", "outra.xlsx - Excel", false);
        servico.Iniciar();

        Assert.Equal("", repositorio.Listar()[0].Escritorio);

        leitor.Proxima = new AmostraDeJanela("Excel", "planilha secreta.xlsx - Excel", false);
        servico.Pulso();

        Assert.Equal("Centro", repositorio.Listar()[1].Escritorio);
    }

    [Fact]
    public void Sem_janela_nao_abre_intervalo()
    {
        var (servico, leitor, repositorio, _, _) = Cenario.Criar();
        leitor.Proxima = null;

        servico.Iniciar();

        Assert.True(servico.EmLeitura);
        Assert.Empty(repositorio.Listar());
    }

    [Fact]
    public void Falha_de_leitura_nao_abre_intervalo()
    {
        var (servico, leitor, repositorio, _, _) = Cenario.Criar();
        leitor.Falha = new InvalidOperationException("negado");

        servico.Iniciar();

        Assert.True(servico.EmLeitura);
        Assert.Empty(repositorio.Listar());
    }

    [Fact]
    public void Resumo_agrupa_e_marca_se_esta_lendo()
    {
        var (servico, leitor, _, relogio, _) = Cenario.Criar();
        leitor.Proxima = new AmostraDeJanela("Word", "relatorio.docx - Word", false);
        servico.Iniciar();
        PassarTempo(servico, relogio, TimeSpan.FromMinutes(10));
        leitor.Proxima = new AmostraDeJanela("Chrome", "Caixa - Google Chrome", false);
        servico.Pulso();
        relogio.Instante = relogio.Instante.AddMinutes(4);

        var resumo = servico.ConsultarResumo();

        Assert.True(resumo.EmLeitura);
        Assert.Equal("ter. 17/11", resumo.Nos[0].Rotulo);
        Assert.Equal("Chrome", resumo.Nos[0].Filhos[0].Rotulo);
        Assert.Equal("Caixa", resumo.Nos[0].Filhos[0].Filhos[0].Rotulo);
        Assert.Equal(TimeSpan.FromMinutes(4), resumo.Nos[0].Filhos[0].DuracaoTotal);
        Assert.Equal("Word", resumo.Nos[0].Filhos[1].Rotulo);
        Assert.Equal(TimeSpan.FromMinutes(10), resumo.Nos[0].Filhos[1].DuracaoTotal);
    }

    [Fact]
    public void Troca_de_aba_no_painel_fecha_o_intervalo()
    {
        var (servico, leitor, repositorio, _, _) = Cenario.Criar();
        leitor.Proxima = new AmostraDeJanela("Google Chrome", "SplitC", false, Enderecos.Fontes);
        servico.Iniciar();

        leitor.Proxima = new AmostraDeJanela("Google Chrome", "SplitC", false, Enderecos.Calculo);
        servico.Pulso();

        var lista = repositorio.Listar();
        Assert.Equal(2, lista.Count);
        Assert.Equal("Painel SplitC", lista[0].Programa);
        Assert.Equal("2200", lista[0].Escritorio);
        Assert.Equal("17927", lista[0].Plano);
        Assert.Equal("49797", lista[0].Periodo);
        Assert.Equal("Fontes", lista[0].Sessao);
        Assert.Equal("862247", lista[0].Item);
        Assert.Equal("Cálculos", lista[1].Sessao);
        Assert.Equal("596648", lista[1].Item);
        Assert.NotNull(lista[0].Fim);
        Assert.Null(lista[1].Fim);
    }

    [Fact]
    public void Mover_a_celula_nao_abre_outro_intervalo()
    {
        var (servico, leitor, repositorio, _, _) = Cenario.Criar();
        leitor.Proxima = new AmostraDeJanela("Google Chrome", "SplitC", false, Enderecos.Fontes);
        servico.Iniciar();

        leitor.Proxima = new AmostraDeJanela("Google Chrome", "SplitC", false, Enderecos.FontesOutraCelula);
        servico.Pulso();

        Assert.Equal(1, repositorio.Vistos);
        var intervalo = Assert.Single(repositorio.Listar());
        Assert.Equal("Fontes", intervalo.Sessao);
        Assert.Equal("862247", intervalo.Item);
        Assert.Null(intervalo.Fim);
    }

    [Fact]
    public void Endereco_ilegivel_com_o_mesmo_titulo_mantem_o_painel()
    {
        var (servico, leitor, repositorio, _, _) = Cenario.Criar();
        leitor.Proxima = new AmostraDeJanela("Google Chrome", "SplitC", false, Enderecos.Fontes);
        servico.Iniciar();

        leitor.Proxima = new AmostraDeJanela("Google Chrome", "SplitC", false, "");
        servico.Pulso();

        var intervalo = Assert.Single(repositorio.Listar());
        Assert.Equal("Painel SplitC", intervalo.Programa);
        Assert.Null(intervalo.Fim);
    }

    [Fact]
    public void Outra_pagina_fecha_o_painel()
    {
        var (servico, leitor, repositorio, _, _) = Cenario.Criar();
        leitor.Proxima = new AmostraDeJanela("Google Chrome", "SplitC", false, Enderecos.Fontes);
        servico.Iniciar();

        leitor.Proxima = new AmostraDeJanela("Google Chrome", "Gmail", false, "https://mail.google.com/mail");
        servico.Pulso();

        var lista = repositorio.Listar();
        Assert.Equal("Painel SplitC", lista[0].Programa);
        Assert.NotNull(lista[0].Fim);
        Assert.Equal("Google Chrome", lista[1].Programa);
        Assert.Equal("Gmail", lista[1].Contexto);
        Assert.Equal("", lista[1].Escritorio);
    }

    [Fact]
    public void Programa_pausado_nao_abre_intervalo_e_fecha_o_que_estava_aberto()
    {
        var (servico, leitor, repositorio, _, _) = Cenario.Criar();
        leitor.Proxima = new AmostraDeJanela("Google Chrome", "Gmail", false);
        servico.Iniciar();

        servico.AlternarPausado("Google Chrome");
        leitor.Proxima = new AmostraDeJanela("Google Chrome", "Agenda", false);
        servico.Pulso();

        var intervalo = Assert.Single(repositorio.Listar());
        Assert.NotNull(intervalo.Fim);
        Assert.Equal("Gmail", intervalo.Contexto);
    }

    [Fact]
    public void Pausar_o_chrome_nao_pausa_o_painel()
    {
        var (servico, leitor, repositorio, _, _) = Cenario.Criar();
        servico.AlternarPausado("Google Chrome");
        leitor.Proxima = new AmostraDeJanela("Google Chrome", "SplitC", false, Enderecos.Fontes);
        servico.Iniciar();

        var intervalo = Assert.Single(repositorio.Listar());
        Assert.Equal("Painel SplitC", intervalo.Programa);
        Assert.Null(intervalo.Fim);
    }

    [Fact]
    public void Pausar_o_painel_deixa_o_chrome_seguir()
    {
        var (servico, leitor, repositorio, _, _) = Cenario.Criar();
        servico.AlternarPausado("Painel SplitC");
        leitor.Proxima = new AmostraDeJanela("Google Chrome", "SplitC", false, Enderecos.Fontes);
        servico.Iniciar();
        Assert.Empty(repositorio.Listar());

        leitor.Proxima = new AmostraDeJanela("Google Chrome", "Gmail", false, "https://mail.google.com/mail");
        servico.Pulso();

        var intervalo = Assert.Single(repositorio.Listar());
        Assert.Equal("Google Chrome", intervalo.Programa);
    }

    [Fact]
    public void Excluir_apaga_so_o_programa_escolhido()
    {
        var (servico, leitor, repositorio, relogio, _) = Cenario.Criar();
        leitor.Proxima = new AmostraDeJanela("Word", "a.docx - Word", false);
        servico.Iniciar();
        PassarTempo(servico, relogio, TimeSpan.FromMinutes(2));
        leitor.Proxima = new AmostraDeJanela("Google Chrome", "Gmail", false);
        servico.Pulso();

        servico.ExcluirPrograma("Google Chrome");

        var intervalo = Assert.Single(repositorio.Listar());
        Assert.Equal("Word", intervalo.Programa);
    }

    [Fact]
    public void Programa_fixado_fica_na_frente()
    {
        var (servico, leitor, _, relogio, _) = Cenario.Criar();
        leitor.Proxima = new AmostraDeJanela("Word", "a.docx - Word", false);
        servico.Iniciar();
        PassarTempo(servico, relogio, TimeSpan.FromMinutes(2));
        leitor.Proxima = new AmostraDeJanela("Google Chrome", "Gmail", false);
        servico.Pulso();
        servico.AlternarFixado("Word");

        var resumo = servico.ConsultarResumo();

        Assert.Equal("ter. 17/11", resumo.Nos[0].Rotulo);
        Assert.Equal("Word", resumo.Nos[0].Filhos[0].Rotulo);
        Assert.Equal("Google Chrome", resumo.Nos[0].Filhos[1].Rotulo);
        Assert.True(resumo.Preferencias["Word"].Fixado);
    }

    [Fact]
    public void Zerar_hoje_apaga_o_de_hoje_e_guarda_o_de_ontem()
    {
        var (servico, leitor, repositorio, relogio, _) = Cenario.Criar();
        relogio.Instante = new DateTimeOffset(2026, 11, 16, 10, 0, 0, TimeSpan.FromHours(-3));
        leitor.Proxima = new AmostraDeJanela("Word", "ontem.docx - Word", false);
        servico.Iniciar();
        relogio.Instante = relogio.Instante.AddHours(1);
        servico.Parar();

        relogio.Instante = new DateTimeOffset(2026, 11, 17, 10, 0, 0, TimeSpan.FromHours(-3));
        leitor.Proxima = new AmostraDeJanela("Chrome", "hoje", false);
        servico.Iniciar();
        relogio.Instante = relogio.Instante.AddMinutes(20);
        servico.Parar();

        var hoje = PeriodoLocal.Hoje(relogio.Instante);
        servico.ZerarPeriodo(hoje.Inicio, hoje.Fim);

        var restante = Assert.Single(repositorio.Listar());
        Assert.Equal("Word", restante.Programa);
        Assert.Equal(new DateTimeOffset(2026, 11, 16, 10, 0, 0, TimeSpan.FromHours(-3)), restante.Inicio);
        Assert.Equal(new DateTimeOffset(2026, 11, 16, 11, 0, 0, TimeSpan.FromHours(-3)), restante.Fim);
    }

    [Fact]
    public void Filtro_do_dia_esconde_os_outros_dias()
    {
        var (servico, leitor, _, relogio, _) = Cenario.Criar();
        relogio.Instante = new DateTimeOffset(2026, 11, 16, 12, 0, 0, TimeSpan.FromHours(-3));
        leitor.Proxima = new AmostraDeJanela("Word", "ontem.docx - Word", false);
        servico.Iniciar();
        relogio.Instante = relogio.Instante.AddMinutes(10);
        servico.Parar();

        relogio.Instante = new DateTimeOffset(2026, 11, 17, 12, 0, 0, TimeSpan.FromHours(-3));
        leitor.Proxima = new AmostraDeJanela("Chrome", "hoje", false);
        servico.Iniciar();
        servico.Parar();

        var hoje = new DateTime(2026, 11, 17);
        var resumo = servico.ConsultarResumo(hoje, hoje);

        var dia = Assert.Single(resumo.Nos);
        Assert.Equal("ter. 17/11", dia.Rotulo);
        Assert.Equal("Chrome", Assert.Single(dia.Filhos).Rotulo);
        Assert.True(resumo.TemRegistros);
    }

    [Fact]
    public void Relatorio_de_escritorios_respeita_o_dia_filtrado()
    {
        var (servico, leitor, _, relogio, _) = Cenario.Criar();
        relogio.Instante = new DateTimeOffset(2026, 11, 16, 12, 0, 0, TimeSpan.FromHours(-3));
        leitor.Proxima = new AmostraDeJanela("Google Chrome", "SplitC", false, Enderecos.Fontes);
        servico.Iniciar();
        relogio.Instante = relogio.Instante.AddMinutes(10);
        servico.Parar();

        relogio.Instante = new DateTimeOffset(2026, 11, 17, 12, 0, 0, TimeSpan.FromHours(-3));
        leitor.Proxima = new AmostraDeJanela("Google Chrome", "SplitC", false, Enderecos.Calculo);
        servico.Iniciar();
        relogio.Instante = relogio.Instante.AddMinutes(4);
        servico.Parar();

        var hoje = new DateTime(2026, 11, 17);
        var linha = Assert.Single(servico.ConsultarEscritorios(hoje, hoje));

        Assert.Equal("Escritório 2200", linha.Rotulo);
        Assert.Equal(TimeSpan.FromMinutes(4), linha.DuracaoPainel);
    }

    [Fact]
    public void Nome_e_variacao_colocam_a_reuniao_no_escritorio()
    {
        var (servico, leitor, _, relogio, _) = Cenario.Criar();
        relogio.Instante = new DateTimeOffset(2026, 11, 17, 12, 0, 0, TimeSpan.FromHours(-3));
        leitor.Proxima = new AmostraDeJanela(
            "Google Chrome",
            "Meet - NTM <> SplitC - Alinhamento - Google Chrome",
            false);
        servico.Iniciar();
        PassarTempo(servico, relogio, TimeSpan.FromMinutes(6));
        leitor.Proxima = new AmostraDeJanela("Google Chrome", "SplitC", false, Enderecos.Fontes);
        leitor.Abertas.Add(new JanelaAberta(
            "Google Chrome",
            "SplitC",
            false,
            Navegador: true,
            Abas: ["Meet - NTM <> SplitC - Alinhamento", "SplitC"]));
        servico.Pulso();
        PassarTempo(servico, relogio, TimeSpan.FromMinutes(4));
        leitor.Proxima = new AmostraDeJanela(
            "Google Chrome",
            "Meet - NTM Sul <> SplitC - Daily - Google Chrome",
            false);
        servico.Pulso();
        PassarTempo(servico, relogio, TimeSpan.FromMinutes(3));
        servico.Parar();

        var hoje = new DateTime(2026, 11, 17);
        Assert.Equal(3, servico.ConsultarEscritorios(hoje, hoje).Count);

        servico.DefinirNomeDoEscritorio("2200", "NTM");
        servico.DefinirNomeDoPlano("2200", "17927", "Comissão");
        Assert.Equal("O nome do escritório já cobre essa variação.", servico.AdicionarVariacao("2200", "ntm"));
        Assert.Null(servico.AdicionarVariacao("2200", "NTM Sul"));
        Assert.Equal("Essa variação já está neste escritório.", servico.AdicionarVariacao("2200", "ntm sul"));

        var linha = Assert.Single(servico.ConsultarEscritorios(hoje, hoje), item => item.Id == "2200");
        Assert.Equal("NTM", linha.Rotulo);
        Assert.Equal(TimeSpan.Zero, linha.DuracaoPainel);
        Assert.Equal(TimeSpan.FromMinutes(4), linha.DuracaoPainelNaReuniao);
        Assert.Equal(TimeSpan.FromMinutes(13), linha.DuracaoReunioes);
        Assert.Equal(["Alinhamento", "Daily"], linha.Temas.Select(tema => tema.Tema));
        Assert.Equal("Comissão", Assert.Single(linha.Planos).Nome);

        var dia = Assert.Single(servico.ConsultarResumo(hoje, hoje).Nos);
        var painel = Assert.Single(dia.Filhos, no => no.Rotulo == "Painel SplitC");
        Assert.Equal("NTM", painel.Filhos[0].Rotulo);
        Assert.Equal("Comissão", painel.Filhos[0].Filhos[0].Rotulo);
        var folha = painel;
        while (folha.Filhos.Count > 0)
            folha = Assert.Single(folha.Filhos);
        Assert.True(Assert.Single(folha.Intervalos).DuranteReuniao);
    }

    [Fact]
    public void Reuniao_continua_sem_depender_do_foco()
    {
        var (servico, leitor, repositorio, relogio, _) = Cenario.Criar();
        var titulo = "Meet: vct <> splitc - Google Chrome";
        leitor.Proxima = new AmostraDeJanela("Google Chrome", titulo, false);
        servico.Iniciar();
        var id = Assert.Single(repositorio.Listar()).Id;

        leitor.Proxima = new AmostraDeJanela("Google Chrome", titulo, true);
        servico.Pulso();
        Assert.Equal(id, Assert.Single(repositorio.Listar()).Id);
        Assert.Null(repositorio.ObterReuniaoEmSegundoPlano());

        PassarTempo(servico, relogio, TimeSpan.FromMinutes(4));
        leitor.Proxima = new AmostraDeJanela("Word", "notas.docx - Word", false);
        leitor.ReuniaoMinimizada = new AmostraDeJanela("Google Chrome", titulo, true);
        servico.Pulso();
        PassarTempo(servico, relogio, TimeSpan.FromMinutes(6));

        var fundo = repositorio.ObterReuniaoEmSegundoPlano();
        Assert.NotNull(fundo);
        Assert.Equal(id, fundo.Id);
        Assert.Null(fundo.Fim);
        Assert.Equal(2, repositorio.Listar().Count);

        leitor.ReuniaoMinimizada = null;
        leitor.Proxima = new AmostraDeJanela("Google Chrome", "Meet: mgs-jeuy-unz - Google Chrome", false);
        servico.Pulso();
        Assert.Equal(id, repositorio.ObterAberto()!.Id);
        Assert.Equal(2, repositorio.Listar().Count);

        leitor.Proxima = new AmostraDeJanela("Google Chrome", titulo, false);
        servico.Pulso();
        Assert.Equal(id, repositorio.ObterAberto()!.Id);

        servico.DefinirNomeDoEscritorio("1905", "VCT");
        var hoje = new DateTime(2026, 11, 17);
        var linha = Assert.Single(servico.ConsultarEscritorios(hoje, hoje));
        Assert.Equal(TimeSpan.FromMinutes(10), linha.DuracaoReunioes);
        var dia = Assert.Single(servico.ConsultarResumo(hoje, hoje).Nos);
        var chrome = Assert.Single(dia.Filhos, no => no.Rotulo == "Google Chrome");
        Assert.Equal(TimeSpan.FromMinutes(10), chrome.DuracaoTotal);
        Assert.Single(Assert.Single(chrome.Filhos).Intervalos);
        Assert.Equal(TimeSpan.FromMinutes(6), Assert.Single(dia.Filhos, no => no.Rotulo == "Word").DuracaoTotal);

        PassarTempo(servico, relogio, TimeSpan.FromMinutes(1));
        var ultimaVista = relogio.Instante;

        // Outra aba tomou a frente no navegador e as abas não puderam ser lidas: vale a folga de uma hora.
        leitor.Abertas.Add(new JanelaAberta("Google Chrome", "Gmail - Google Chrome", false, Navegador: true));
        leitor.Proxima = new AmostraDeJanela("Discord", "amigos - Discord", false);
        servico.Pulso();
        Assert.Equal(id, repositorio.ObterReuniaoEmSegundoPlano()!.Id);
        Assert.Null(repositorio.ObterReuniaoEmSegundoPlano()!.Fim);

        PassarTempo(servico, relogio, TimeSpan.FromMinutes(50));
        Assert.Null(repositorio.ObterReuniaoEmSegundoPlano()!.Fim);
        PassarTempo(servico, relogio, TimeSpan.FromMinutes(11));
        var encerrada = Assert.Single(repositorio.Listar(), item => item.Id == id);
        Assert.Equal(ultimaVista, encerrada.Fim);
        Assert.Null(repositorio.ObterReuniaoEmSegundoPlano());
    }

    [Fact]
    public void Reuniao_segue_contando_quando_um_programa_pausado_vem_para_a_frente()
    {
        var (servico, leitor, repositorio, relogio, _) = Cenario.Criar();
        var titulo = "Meet: vct <> splitc - Google Chrome";
        servico.AlternarPausado("Esforço");
        leitor.Proxima = new AmostraDeJanela("Google Chrome", titulo, false);
        leitor.Abertas.Add(new JanelaAberta("Google Chrome", titulo, false, Navegador: true));
        servico.Iniciar();
        var id = Assert.Single(repositorio.Listar()).Id;

        PassarTempo(servico, relogio, TimeSpan.FromMinutes(1));
        leitor.Proxima = new AmostraDeJanela("Esforço", "Esforço", false, Propria: true);
        PassarTempo(servico, relogio, TimeSpan.FromMinutes(2));

        var reuniao = Assert.Single(repositorio.Listar());
        Assert.Equal(id, reuniao.Id);
        Assert.Null(reuniao.Fim);
        Assert.True(reuniao.EmSegundoPlano);

        leitor.Proxima = new AmostraDeJanela("Google Chrome", titulo, false);
        PassarTempo(servico, relogio, TimeSpan.FromMinutes(1));

        reuniao = Assert.Single(repositorio.Listar());
        Assert.Equal(id, reuniao.Id);
        Assert.Null(reuniao.Fim);
        Assert.False(reuniao.EmSegundoPlano);
        Assert.Equal(TimeSpan.FromMinutes(4), reuniao.DuracaoAte(relogio.Instante));
    }

    [Fact]
    public void Pausar_o_programa_da_reuniao_encerra_a_reuniao()
    {
        var (servico, leitor, repositorio, relogio, _) = Cenario.Criar();
        var titulo = "Meet: vct <> splitc - Google Chrome";
        leitor.Proxima = new AmostraDeJanela("Google Chrome", titulo, false);
        leitor.Abertas.Add(new JanelaAberta("Google Chrome", titulo, false, Navegador: true));
        servico.Iniciar();
        PassarTempo(servico, relogio, TimeSpan.FromMinutes(1));
        leitor.Proxima = new AmostraDeJanela("Word", "notas.docx - Word", false);
        PassarTempo(servico, relogio, TimeSpan.FromMinutes(1));
        var pausa = relogio.Instante;

        servico.AlternarPausado("Google Chrome");
        PassarTempo(servico, relogio, TimeSpan.FromMinutes(1));

        var reuniao = Assert.Single(repositorio.Listar(), item => item.Programa == "Google Chrome");
        Assert.Equal(pausa, reuniao.Fim);
        Assert.Null(Assert.Single(repositorio.Listar(), item => item.Programa == "Word").Fim);
    }

    [Fact]
    public void Chatwoot_grava_o_escritorio_do_chat_aberto()
    {
        var (servico, leitor, repositorio, relogio, _) = Cenario.Criar();
        servico.DefinirNomeDoEscritorio("1905", "Concha y Toro");
        servico.DefinirNomeDoEscritorio("1759", "Straumann");
        Assert.Null(servico.AdicionarVariacao("1905", "VCT"));
        var endereco = "https://ticketing.splitc.com.br/app/accounts/1/inbox-view/conversation/22736";
        leitor.Proxima = new AmostraDeJanela(
            "Google Chrome",
            "Chatwoot - Google Chrome",
            false,
            endereco,
            "SplitC <> Concha y Toro - VCT");

        servico.Iniciar();

        var aberto = repositorio.ObterAberto();
        Assert.NotNull(aberto);
        Assert.Equal("Chatwoot", aberto.Programa);
        Assert.Equal("1905", aberto.Escritorio);
        Assert.Equal("SplitC <> Concha y Toro - VCT", aberto.Contexto);

        PassarTempo(servico, relogio, TimeSpan.FromMinutes(4));
        leitor.Proxima = new AmostraDeJanela("Google Chrome", "Chatwoot - Google Chrome", false, endereco, "");
        servico.Pulso();
        Assert.Equal(aberto.Id, repositorio.ObterAberto()!.Id);

        PassarTempo(servico, relogio, TimeSpan.FromMinutes(2));
        leitor.Proxima = new AmostraDeJanela(
            "Google Chrome",
            "Chatwoot - Google Chrome",
            false,
            endereco,
            "SplitC <> Straumann");
        servico.Pulso();

        var lista = repositorio.Listar();
        Assert.Equal(2, lista.Count);
        Assert.Equal("Chatwoot", lista[0].Programa);
        Assert.Equal("Chatwoot", lista[1].Programa);
        Assert.Equal("1905", lista[0].Escritorio);
        Assert.Equal("1759", lista[1].Escritorio);
        Assert.Equal(relogio.Instante, lista[0].Fim);
    }

    [Fact]
    public void Reuniao_retomada_em_menos_de_uma_hora_conta_o_tempo_do_meio()
    {
        var (servico, leitor, repositorio, relogio, _) = Cenario.Criar();
        var titulo = "Meet: vct <> splitc - Google Chrome";
        leitor.Proxima = new AmostraDeJanela("Google Chrome", titulo, false);
        servico.Iniciar();
        PassarTempo(servico, relogio, TimeSpan.FromMinutes(18));
        servico.Parar();

        relogio.Instante = relogio.Instante.AddMinutes(22);
        servico.Iniciar();
        PassarTempo(servico, relogio, TimeSpan.FromMinutes(1));

        servico.DefinirNomeDoEscritorio("1905", "VCT");
        var hoje = new DateTime(2026, 11, 17);
        var linha = Assert.Single(servico.ConsultarEscritorios(hoje, hoje));
        Assert.Equal(TimeSpan.FromMinutes(41), linha.DuracaoReunioes);

        var dia = Assert.Single(servico.ConsultarResumo(hoje, hoje).Nos);
        var chrome = Assert.Single(dia.Filhos, no => no.Rotulo == "Google Chrome");
        Assert.Single(Assert.Single(chrome.Filhos).Intervalos);
        Assert.Equal(2, repositorio.Listar().Count);
    }

    [Fact]
    public void Buraco_no_relogio_fecha_no_ultimo_pulso_e_nao_emenda_a_reuniao()
    {
        var (servico, leitor, repositorio, relogio, _) = Cenario.Criar();
        var titulo = "Meet: vct <> splitc - Google Chrome";
        leitor.Proxima = new AmostraDeJanela("Google Chrome", titulo, false);
        servico.Iniciar();
        PassarTempo(servico, relogio, TimeSpan.FromMinutes(1));
        var corte = relogio.Instante;

        relogio.Instante = corte.Add(ServicoDeEsforco.FolgaDeAusencia).AddSeconds(5);
        servico.Pulso();

        var lista = repositorio.Listar().OrderBy(item => item.Inicio).ToList();
        Assert.Equal(2, lista.Count);
        Assert.Equal(corte, lista[0].Fim);
        Assert.True(lista[1].Retomado);
        Assert.Equal(relogio.Instante, lista[1].Inicio);
        Assert.Null(lista[1].Fim);

        PassarTempo(servico, relogio, TimeSpan.FromMinutes(2));
        servico.Parar();
        servico.DefinirNomeDoEscritorio("1905", "VCT");

        var hoje = new DateTime(2026, 11, 17);
        var linha = Assert.Single(servico.ConsultarEscritorios(hoje, hoje));
        Assert.Equal(TimeSpan.FromMinutes(3), linha.DuracaoReunioes);
    }

    [Fact]
    public void Suspenso_o_pulso_nao_le_nem_abre_intervalo()
    {
        var (servico, leitor, repositorio, relogio, _) = Cenario.Criar();
        leitor.Proxima = new AmostraDeJanela("Word", "a.docx - Word", false);
        servico.Iniciar();
        var leituras = leitor.Leituras;

        servico.Ausentar();
        Assert.NotNull(Assert.Single(repositorio.Listar()).Fim);

        relogio.Instante = relogio.Instante.AddMinutes(5);
        leitor.Proxima = new AmostraDeJanela("Excel", "b.xlsx - Excel", false);
        servico.Pulso();
        Assert.Equal(leituras, leitor.Leituras);
        Assert.Single(repositorio.Listar());

        servico.Parar();
        servico.Iniciar();
        Assert.Equal(leituras, leitor.Leituras);
        Assert.True(servico.EmLeitura);

        servico.Retomar();
        servico.Pulso();
        Assert.Equal(leituras + 1, leitor.Leituras);
        var aberto = repositorio.ObterAberto();
        Assert.NotNull(aberto);
        Assert.Equal("Excel", aberto.Programa);
    }

    private static void PassarTempo(ServicoDeEsforco servico, RelogioManual relogio, TimeSpan quanto)
    {
        var destino = relogio.Instante + quanto;
        var passo = TimeSpan.FromSeconds(5);
        while (relogio.Instante + passo < destino)
        {
            relogio.Instante += passo;
            servico.Pulso();
        }

        if (relogio.Instante < destino)
        {
            relogio.Instante = destino;
            servico.Pulso();
        }
    }
}

file static class Enderecos
{
    public const string Fontes = "https://app.splitc.com.br/planos/17927/tab/862247?officeId=2200&periodId=49797&tabSelector=source&pagina=0&coluna=bq4&linha=19&rowId=19";
    public const string FontesOutraCelula = "https://app.splitc.com.br/planos/17927/tab/862247?officeId=2200&periodId=49797&tabSelector=source&pagina=1&coluna=c1&linha=3&rowId=3";
    public const string Calculo = "https://app.splitc.com.br/planos/17927/tab/596648?officeId=2200&periodId=49797&tabSelector=plans&pagina=0&coluna=bq4&linha=19&rowId=19";
}
