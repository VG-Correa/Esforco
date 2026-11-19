using Esforco.Dominio;

namespace Esforco.Testes.Dominio;

public class CalendarioDeEsforcoTestes
{
    private static readonly TimeSpan Zona = TimeSpan.FromHours(-3);

    private static DateTimeOffset Em(int dia, int hora, int minuto) =>
        new(2026, 11, dia, hora, minuto, 0, Zona);

    [Fact]
    public void Escritorio_pinta_o_nome_e_o_que_foi_feito_e_deixa_o_resto_de_fora()
    {
        var painel = Painel("1905", Em(17, 10, 0), Em(17, 10, 30));
        var reuniao = Reuniao("VCT <> SplitC - Alinhamento", Em(17, 10, 0), Em(17, 11, 0));
        var chrome = IntervaloDeEsforco.Abrir("Google Chrome", "Gmail - Google Chrome", "Gmail", "", false, Em(17, 12, 0));
        chrome.Fechar(Em(17, 12, 20));
        var nomes = new NomesDePainel([new CadastroDeEscritorio("1905", "Concha y Toro", ["VCT"], [])]);

        var quadro = CalendarioDeEsforco.Montar(
            [painel, reuniao, chrome],
            new DateTime(2026, 11, 17),
            new DateTime(2026, 11, 17),
            Em(17, 18, 0),
            ModoDoCalendario.Escritorios,
            nomes,
            false,
            false);

        var dia = Assert.Single(quadro.Dias);
        Assert.False(quadro.Vertical);
        var bloco = Assert.Single(dia.Blocos);
        Assert.Equal("Concha y Toro", bloco.Titulo);
        Assert.Equal("Alinhamento · Fontes", bloco.Subtitulo);
        Assert.Equal("Reunião · Painel", bloco.Tipo);
        Assert.StartsWith("Reunião · Painel", bloco.Dica);
        Assert.Equal(10 * 60, bloco.InicioMinuto);
        Assert.Equal(11 * 60, bloco.FimMinuto);
        Assert.Equal(8, quadro.HoraInicial);
        Assert.Equal(18, quadro.HoraFinal);
    }

    [Fact]
    public void Trechos_colados_do_mesmo_feito_viram_uma_faixa()
    {
        var primeiro = Painel("1905", Em(17, 9, 0), Em(17, 9, 30));
        var segundo = Painel("1905", Em(17, 9, 30), Em(17, 10, 0));
        var nomes = new NomesDePainel([new CadastroDeEscritorio("1905", "Concha y Toro", [], [])]);

        var dia = DiaUnico([primeiro, segundo], nomes);

        var bloco = Assert.Single(dia.Blocos);
        Assert.Equal(9 * 60, bloco.InicioMinuto);
        Assert.Equal(10 * 60, bloco.FimMinuto);
        Assert.Equal(0, bloco.Trilha);
    }

    [Fact]
    public void Cliques_rapidos_no_mesmo_escritorio_viram_uma_faixa()
    {
        var nomes = new NomesDePainel([new CadastroDeEscritorio("1905", "Concha y Toro", [], [])]);
        var inicio = Em(17, 10, 0);
        var fatias = new List<IntervaloDeEsforco>();
        for (var i = 0; i < 8; i++)
        {
            var comeco = inicio.AddSeconds(i * 2);
            var sessao = i % 2 == 0 ? "Dashboards" : "Fontes";
            fatias.Add(PainelEm("1905", sessao, i.ToString(), comeco, comeco.AddSeconds(2)));
        }

        fatias.Add(PainelEm("1905", "Dashboards", "fim", inicio.AddSeconds(16), Em(17, 10, 25)));

        var bloco = Assert.Single(DiaUnico(fatias, nomes).Blocos);
        Assert.Equal("Concha y Toro", bloco.Titulo);
        Assert.Equal("Dashboards", bloco.Subtitulo);
        Assert.Equal(0, bloco.Trilha);
    }

    [Fact]
    public void Escritorios_ao_mesmo_tempo_ficam_em_faixas_paralelas()
    {
        var vct = Painel("1905", Em(17, 10, 0), Em(17, 11, 0));
        var straumann = Painel("1759", Em(17, 10, 30), Em(17, 11, 30));
        var nomes = new NomesDePainel(
        [
            new CadastroDeEscritorio("1905", "Concha y Toro", [], []),
            new CadastroDeEscritorio("1759", "Straumann", [], []),
        ]);

        var dia = DiaUnico([vct, straumann], nomes);
        Assert.Equal(2, dia.Blocos.Count);
        Assert.Contains(dia.Blocos, bloco => bloco.Titulo == "Concha y Toro");
        Assert.Contains(dia.Blocos, bloco => bloco.Titulo == "Straumann");
        Assert.NotEqual(dia.Blocos[0].Trilha, dia.Blocos[1].Trilha);
    }

    [Fact]
    public void Bloco_marca_painel_chatwoot_e_reuniao()
    {
        var painel = Painel("1905", Em(17, 10, 0), Em(17, 10, 20));
        var chat = IntervaloDeEsforco.Abrir("Chatwoot", "SplitC <> Straumann", "", "1759", false, Em(17, 11, 0));
        chat.Fechar(Em(17, 11, 15));
        var reuniao = Reuniao("Galena <> SplitC - Refatoração", Em(17, 15, 0), Em(17, 16, 0));
        var nomes = new NomesDePainel(
        [
            new CadastroDeEscritorio("1905", "Concha y Toro", [], []),
            new CadastroDeEscritorio("1759", "Straumann", [], []),
        ]);

        var dia = DiaUnico([painel, chat, reuniao], nomes);

        Assert.Contains(dia.Blocos, bloco => bloco.Titulo == "Concha y Toro" && bloco.Tipo == "Painel");
        Assert.Contains(dia.Blocos, bloco => bloco.Titulo == "Straumann" && bloco.Tipo == "Chatwoot");
        Assert.Contains(dia.Blocos, bloco => bloco.Tipo == "Reunião" && bloco.Dica.StartsWith("Reunião"));
    }

    [Fact]
    public void Mesmo_escritorio_volta_em_quatro_minutos_e_vira_um_bloco()
    {
        var primeiro = Painel("1905", Em(17, 10, 0), Em(17, 10, 2));
        var segundo = Painel("1905", Em(17, 10, 6), Em(17, 10, 8));
        var nomes = new NomesDePainel([new CadastroDeEscritorio("1905", "Concha y Toro", [], [])]);

        var bloco = Assert.Single(DiaUnico([primeiro, segundo], nomes).Blocos);

        Assert.Equal(10 * 60, bloco.InicioMinuto);
        Assert.Equal(10 * 60 + 8, bloco.FimMinuto);
        Assert.Equal(10 * 3600, bloco.InicioSegundo);
        Assert.Equal(10 * 3600 + 8 * 60, bloco.FimSegundo);
    }

    [Fact]
    public void Vao_de_seis_minutos_mantem_dois_blocos()
    {
        var primeiro = Painel("1905", Em(17, 10, 0), Em(17, 10, 2));
        var segundo = Painel("1905", Em(17, 10, 8), Em(17, 10, 10));
        var nomes = new NomesDePainel([new CadastroDeEscritorio("1905", "Concha y Toro", [], [])]);

        var dia = DiaUnico([primeiro, segundo], nomes);

        Assert.Equal(2, dia.Blocos.Count);
        Assert.Equal(10 * 60, dia.Blocos[0].InicioMinuto);
        Assert.Equal(10 * 60 + 8, dia.Blocos[1].InicioMinuto);
    }

    [Fact]
    public void Escritorios_intercalados_viram_dois_cards_sobrepostos()
    {
        var nomes = new NomesDePainel(
        [
            new CadastroDeEscritorio("1905", "Concha y Toro", [], []),
            new CadastroDeEscritorio("1759", "Straumann", [], []),
        ]);
        var fatias = new List<IntervaloDeEsforco>();
        for (var i = 0; i < 6; i++)
        {
            var comeco = Em(17, 10, 0).AddMinutes(i * 2);
            var escritorio = i % 2 == 0 ? "1905" : "1759";
            fatias.Add(Painel(escritorio, comeco, comeco.AddMinutes(1)));
        }

        var dia = DiaUnico(fatias, nomes);
        var vct = Assert.Single(dia.Blocos, bloco => bloco.Titulo == "Concha y Toro");
        var straumann = Assert.Single(dia.Blocos, bloco => bloco.Titulo == "Straumann");

        Assert.Equal(10 * 60, vct.InicioMinuto);
        Assert.Equal(10 * 60 + 9, vct.FimMinuto);
        Assert.Equal(10 * 60 + 2, straumann.InicioMinuto);
        Assert.Equal(10 * 60 + 11, straumann.FimMinuto);
        Assert.NotEqual(vct.Trilha, straumann.Trilha);
    }

    [Fact]
    public void Acesso_de_segundos_sem_retorno_permanece()
    {
        var curto = Painel("1905", Em(17, 10, 0), Em(17, 10, 0).AddSeconds(8));
        var nomes = new NomesDePainel([new CadastroDeEscritorio("1905", "Concha y Toro", [], [])]);

        var bloco = Assert.Single(DiaUnico([curto], nomes).Blocos);

        Assert.Equal(10 * 3600, bloco.InicioSegundo);
        Assert.Equal(10 * 3600 + 8, bloco.FimSegundo);
    }

    [Fact]
    public void Programa_e_tela_fundem_a_volta_e_guardam_o_acesso_curto()
    {
        var gmail1 = Programa("Google Chrome", "Gmail", Em(17, 10, 0), Em(17, 10, 2));
        var gmail2 = Programa("Google Chrome", "Gmail", Em(17, 10, 6), Em(17, 10, 8));
        var planilha = Programa("Microsoft Excel", "Planilha", Em(17, 11, 0), Em(17, 11, 0).AddSeconds(8));
        var intervalos = new[] { gmail1, gmail2, planilha };

        var porPrograma = DiaNoModo(intervalos, ModoDoCalendario.Programas);
        var chrome = Assert.Single(porPrograma.Blocos, bloco => bloco.Titulo == "Google Chrome");
        var excel = Assert.Single(porPrograma.Blocos, bloco => bloco.Titulo == "Microsoft Excel");
        Assert.Equal(10 * 3600, chrome.InicioSegundo);
        Assert.Equal(10 * 3600 + 8 * 60, chrome.FimSegundo);
        Assert.Equal(11 * 3600, excel.InicioSegundo);
        Assert.Equal(11 * 3600 + 8, excel.FimSegundo);

        var porTela = DiaNoModo(intervalos, ModoDoCalendario.Telas);
        var gmail = Assert.Single(porTela.Blocos, bloco => bloco.Titulo == "Gmail");
        var folha = Assert.Single(porTela.Blocos, bloco => bloco.Titulo == "Planilha");
        Assert.Equal(10 * 3600, gmail.InicioSegundo);
        Assert.Equal(10 * 3600 + 8 * 60, gmail.FimSegundo);
        Assert.Equal(11 * 3600 + 8, folha.FimSegundo);
    }

    [Fact]
    public void Card_fica_no_segundo_exato()
    {
        var painel = Painel("1905", Em(17, 10, 0).AddSeconds(30), Em(17, 10, 20).AddSeconds(45));
        var nomes = new NomesDePainel([new CadastroDeEscritorio("1905", "Concha y Toro", [], [])]);

        var bloco = Assert.Single(DiaUnico([painel], nomes).Blocos);

        Assert.Equal(10 * 3600 + 30, bloco.InicioSegundo);
        Assert.Equal(10 * 3600 + 20 * 60 + 45, bloco.FimSegundo);
    }

    [Fact]
    public void Reuniao_sem_tema_aparece_como_reuniao()
    {
        var reuniao = Reuniao("vct <> SplitC", Em(17, 14, 0), Em(17, 14, 20));
        var nomes = new NomesDePainel([new CadastroDeEscritorio("1905", "Concha y Toro", ["VCT"], [])]);

        var bloco = Assert.Single(DiaUnico([reuniao], nomes).Blocos);
        Assert.Equal("Concha y Toro", bloco.Titulo);
        Assert.Equal("Reunião", bloco.Subtitulo);
    }

    [Fact]
    public void Um_buraco_mantem_duas_faixas()
    {
        var cedo = Painel("1905", Em(17, 9, 0), Em(17, 9, 10));
        var tarde = Painel("1905", Em(17, 9, 40), Em(17, 10, 0));
        var nomes = new NomesDePainel([new CadastroDeEscritorio("1905", "Concha y Toro", [], [])]);

        var dia = DiaUnico([cedo, tarde], nomes);

        Assert.Equal(2, dia.Blocos.Count);
        Assert.All(dia.Blocos, bloco => Assert.Equal(0, bloco.Trilha));
    }

    [Fact]
    public void Escritorio_oculto_sai_do_calendario()
    {
        var visivel = Painel("1759", Em(17, 9, 0), Em(17, 9, 20));
        var oculto = Painel("1905", Em(17, 11, 0), Em(17, 11, 20));
        var nomes = new NomesDePainel(
        [
            new CadastroDeEscritorio("1905", "Concha y Toro", ["VCT"], []),
            new CadastroDeEscritorio("1759", "Straumann", [], []),
        ]);

        var dia = DiaUnico(
            [visivel, oculto],
            nomes,
            [new OcultacaoDeEscritorio(true, "1905")]);

        var bloco = Assert.Single(dia.Blocos);
        Assert.Equal("Straumann", bloco.Titulo);
    }

    [Fact]
    public void Programa_e_tela_trocam_titulo_e_subtitulo()
    {
        var chrome = IntervaloDeEsforco.Abrir("Google Chrome", "Gmail - Google Chrome", "Gmail", "", false, Em(17, 12, 0));
        chrome.Fechar(Em(17, 12, 15));
        var nomes = NomesDePainel.Vazio;

        var porPrograma = CalendarioDeEsforco.Montar(
            [chrome],
            new DateTime(2026, 11, 17),
            new DateTime(2026, 11, 17),
            Em(17, 18, 0),
            ModoDoCalendario.Programas,
            nomes,
            false,
            false);
        var programa = Assert.Single(Assert.Single(porPrograma.Dias).Blocos);
        Assert.Equal("Google Chrome", programa.Titulo);
        Assert.Equal("Gmail", programa.Subtitulo);

        var porTela = CalendarioDeEsforco.Montar(
            [chrome],
            new DateTime(2026, 11, 17),
            new DateTime(2026, 11, 17),
            Em(17, 18, 0),
            ModoDoCalendario.Telas,
            nomes,
            false,
            false);
        var tela = Assert.Single(Assert.Single(porTela.Dias).Blocos);
        Assert.Equal("Gmail", tela.Titulo);
        Assert.Equal("Google Chrome", tela.Subtitulo);
    }

    [Fact]
    public void Intervalo_que_cruza_a_meia_noite_parte_em_dois_dias()
    {
        var painel = Painel("1905", Em(17, 23, 30), Em(18, 0, 40));
        var nomes = new NomesDePainel([new CadastroDeEscritorio("1905", "Concha y Toro", [], [])]);

        var quadro = CalendarioDeEsforco.Montar(
            [painel],
            new DateTime(2026, 11, 17),
            new DateTime(2026, 11, 18),
            Em(18, 8, 0),
            ModoDoCalendario.Escritorios,
            nomes,
            false,
            false);

        Assert.False(quadro.Vertical);
        Assert.Equal(2, quadro.Dias.Count);
        var ontem = quadro.Dias[0].Blocos.Single();
        var hoje = quadro.Dias[1].Blocos.Single();
        Assert.Equal(23 * 60 + 30, ontem.InicioMinuto);
        Assert.Equal(24 * 60, ontem.FimMinuto);
        Assert.Equal(0, hoje.InicioMinuto);
        Assert.Equal(40, hoje.FimMinuto);
    }

    [Fact]
    public void Semana_lista_os_dias_lado_a_lado()
    {
        var quadro = CalendarioDeEsforco.Montar(
            [],
            new DateTime(2026, 11, 16),
            new DateTime(2026, 11, 20),
            Em(18, 8, 0),
            ModoDoCalendario.Escritorios,
            NomesDePainel.Vazio,
            false,
            false);

        Assert.False(quadro.EmGrade);
        Assert.False(quadro.Vertical);
        Assert.Equal(5, quadro.Dias.Count);
        Assert.Equal(new DateTime(2026, 11, 16), quadro.Dias[0].Dia);
        Assert.Equal(new DateTime(2026, 11, 20), quadro.Dias[^1].Dia);
    }

    [Fact]
    public void Periodo_longo_alinha_os_dias_nas_colunas_da_semana()
    {
        var quadro = CalendarioDeEsforco.Montar(
            [],
            new DateTime(2026, 11, 16),
            new DateTime(2026, 12, 15),
            Em(18, 8, 0),
            ModoDoCalendario.Escritorios,
            NomesDePainel.Vazio,
            false,
            false);

        Assert.True(quadro.EmGrade);
        Assert.False(quadro.Vertical);
        Assert.Equal(new DateTime(2026, 11, 16), quadro.Dias[0].Dia);
        Assert.Equal(new DateTime(2026, 12, 20), quadro.Dias[^1].Dia);
        Assert.Equal(0, quadro.Dias.Count % 7);
    }

    [Fact]
    public void Mes_entra_numa_grade_que_comeca_na_segunda()
    {
        var quadro = CalendarioDeEsforco.Montar(
            [],
            new DateTime(2026, 11, 1),
            new DateTime(2026, 11, 30),
            Em(18, 8, 0),
            ModoDoCalendario.Escritorios,
            NomesDePainel.Vazio,
            true,
            false);

        Assert.True(quadro.EmGrade);
        Assert.Equal(["seg.", "ter.", "qua.", "qui.", "sex.", "sáb.", "dom."], quadro.Cabecalhos);
        Assert.Equal(42, quadro.Dias.Count);
        Assert.Equal(new DateTime(2026, 10, 26), quadro.Dias[0].Dia);
        Assert.False(quadro.Dias[0].NoPeriodo);
        Assert.Contains(quadro.Dias, dia => dia.Dia == new DateTime(2026, 11, 1) && dia.NoPeriodo);
        Assert.Equal(new DateTime(2026, 12, 6), quadro.Dias[^1].Dia);
        Assert.Equal((8, 18), (quadro.HoraInicial, quadro.HoraFinal));
    }

    [Fact]
    public void Grade_de_domingo_a_sabado_comeca_no_dia_primeiro()
    {
        var quadro = CalendarioDeEsforco.Montar(
            [],
            new DateTime(2026, 11, 1),
            new DateTime(2026, 11, 30),
            Em(18, 8, 0),
            ModoDoCalendario.Escritorios,
            NomesDePainel.Vazio,
            true,
            true);

        Assert.Equal(new DateTime(2026, 11, 1), quadro.Dias[0].Dia);
        Assert.True(quadro.Dias[0].NoPeriodo);
        Assert.Equal(35, quadro.Dias.Count);
        Assert.Equal(["dom.", "seg.", "ter.", "qua.", "qui.", "sex.", "sáb."], quadro.Cabecalhos);
    }

    [Fact]
    public void A_janela_de_horas_cobre_o_primeiro_e_o_ultimo_trabalho()
    {
        var cedo = Painel("1905", Em(17, 10, 0), Em(17, 10, 30));
        var tarde = Painel("1905", Em(17, 16, 10), Em(17, 16, 40));
        var nomes = new NomesDePainel([new CadastroDeEscritorio("1905", "Concha y Toro", [], [])]);

        var quadro = CalendarioDeEsforco.Montar(
            [cedo, tarde],
            new DateTime(2026, 11, 16),
            new DateTime(2026, 11, 17),
            Em(17, 18, 0),
            ModoDoCalendario.Escritorios,
            nomes,
            false,
            false);

        Assert.Equal(8, quadro.HoraInicial);
        Assert.Equal(18, quadro.HoraFinal);
        Assert.Equal(2, quadro.Dias.Count);
        Assert.Empty(quadro.Dias[0].Blocos);
    }

    private static DiaDeCalendario DiaNoModo(IEnumerable<IntervaloDeEsforco> intervalos, ModoDoCalendario modo)
    {
        var quadro = CalendarioDeEsforco.Montar(
            intervalos,
            new DateTime(2026, 11, 17),
            new DateTime(2026, 11, 17),
            Em(17, 18, 0),
            modo,
            NomesDePainel.Vazio,
            false,
            false);
        return Assert.Single(quadro.Dias);
    }

    private static IntervaloDeEsforco Programa(string programa, string tela, DateTimeOffset inicio, DateTimeOffset fim)
    {
        var intervalo = IntervaloDeEsforco.Abrir(programa, tela + " - " + programa, tela, "", false, inicio);
        intervalo.Fechar(fim);
        return intervalo;
    }

    private static DiaDeCalendario DiaUnico(
        IEnumerable<IntervaloDeEsforco> intervalos,
        NomesDePainel nomes,
        IEnumerable<OcultacaoDeEscritorio>? ocultos = null)
    {
        var quadro = CalendarioDeEsforco.Montar(
            intervalos,
            new DateTime(2026, 11, 17),
            new DateTime(2026, 11, 17),
            Em(17, 18, 0),
            ModoDoCalendario.Escritorios,
            nomes,
            false,
            false,
            ocultos);
        return Assert.Single(quadro.Dias);
    }

    private static IntervaloDeEsforco Painel(string escritorio, DateTimeOffset inicio, DateTimeOffset fim) =>
        PainelEm(escritorio, "Fontes", "862247", inicio, fim);

    private static IntervaloDeEsforco PainelEm(
        string escritorio,
        string sessao,
        string item,
        DateTimeOffset inicio,
        DateTimeOffset fim)
    {
        var intervalo = IntervaloDeEsforco.AbrirPainel(
            "SplitC",
            new LocalDoPainel(escritorio, "17927", "", sessao, item, TipoDeItem.Aba),
            false,
            inicio);
        intervalo.Fechar(fim);
        return intervalo;
    }

    private static IntervaloDeEsforco Reuniao(string titulo, DateTimeOffset inicio, DateTimeOffset fim)
    {
        var intervalo = IntervaloDeEsforco.Abrir("Google Chrome", titulo, titulo, "", false, inicio);
        intervalo.Fechar(fim);
        return intervalo;
    }
}
