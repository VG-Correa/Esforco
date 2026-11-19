using Esforco.Dominio;

namespace Esforco.Testes.Dominio;

public class RelatorioDeEscritoriosTestes
{
    private static readonly TimeSpan Zona = TimeSpan.FromHours(-3);

    private static DateTimeOffset Em(int dia, int hora, int minuto) =>
        new(2026, 11, dia, hora, minuto, 0, Zona);

    [Fact]
    public void Soma_o_tempo_da_splitc_por_escritorio()
    {
        var primeiro = Painel("2200", Em(17, 9, 0), Em(17, 9, 10));
        var segundo = Painel("2200", Em(17, 10, 0), Em(17, 10, 5));
        var outro = Painel("3300", Em(17, 11, 0), Em(17, 11, 3));
        var chrome = IntervaloDeEsforco.Abrir("Google Chrome", "Gmail", "Gmail", "2200", false, Em(17, 12, 0));
        chrome.Fechar(Em(17, 12, 30));

        var linhas = RelatorioDeEscritorios.Montar([primeiro, segundo, outro, chrome], Em(17, 13, 0));

        Assert.Equal("Escritório 2200", linhas[0].Rotulo);
        Assert.Equal(TimeSpan.FromMinutes(15), linhas[0].DuracaoPainel);
        Assert.Equal(2, linhas[0].IntervalosPainel);
        Assert.Equal("Escritório 3300", linhas[1].Rotulo);
        Assert.Equal(TimeSpan.FromMinutes(3), linhas[1].DuracaoPainel);
    }

    [Fact]
    public void Painel_durante_a_reuniao_do_mesmo_escritorio_fica_de_fora_do_total()
    {
        var reuniao = Reuniao("NTM <> SplitC - Alinhamento", Em(17, 14, 0), Em(17, 15, 0));
        var dentro = Painel("2200", Em(17, 14, 10), Em(17, 14, 25));
        var parcial = Painel("2200", Em(17, 13, 50), Em(17, 14, 10));
        var outro = Painel("3300", Em(17, 14, 20), Em(17, 14, 30));
        var cadastro = new CadastroDeEscritorio("2200", "NTM", [], []);

        var linhas = RelatorioDeEscritorios.Montar(
            [reuniao, dentro, parcial, outro],
            Em(17, 16, 0),
            [cadastro]);

        var ntm = Assert.Single(linhas, linha => linha.Id == "2200");
        Assert.Equal(TimeSpan.FromMinutes(10), ntm.DuracaoPainel);
        Assert.Equal(TimeSpan.FromMinutes(25), ntm.DuracaoPainelNaReuniao);
        Assert.Equal(TimeSpan.FromMinutes(60), ntm.DuracaoReunioes);
        var vizinho = Assert.Single(linhas, linha => linha.Id == "3300");
        Assert.Equal(TimeSpan.FromMinutes(10), vizinho.DuracaoPainel);
        Assert.Equal(TimeSpan.Zero, vizinho.DuracaoPainelNaReuniao);
    }

    [Fact]
    public void Intervalo_aberto_conta_ate_o_instante_pedido()
    {
        var aberto = IntervaloDeEsforco.AbrirPainel("SplitC", Local("2200"), false, Em(17, 9, 0));

        var linha = Assert.Single(RelatorioDeEscritorios.Montar([aberto], Em(17, 9, 8)));

        Assert.Equal(TimeSpan.FromMinutes(8), linha.DuracaoPainel);
    }

    [Fact]
    public void Escritorio_vazio_fica_fora_do_relatorio()
    {
        var intervalo = IntervaloDeEsforco.AbrirPainel("SplitC", Local(""), false, Em(17, 9, 0));
        intervalo.Fechar(Em(17, 9, 2));

        Assert.Empty(RelatorioDeEscritorios.Montar([intervalo], Em(17, 9, 2)));
    }

    [Fact]
    public void Reunioes_do_mesmo_tema_coladas_viram_uma_so()
    {
        var primeira = Reuniao("NTM <> SplitC - Alinhamento", Em(17, 9, 0), Em(17, 9, 10));
        var segunda = Reuniao("Meet - NTM <> SplitC - Alinhamento - Google Chrome", Em(17, 9, 10), Em(17, 9, 25));
        var depois = Reuniao("NTM <> SplitC - Alinhamento", Em(17, 9, 40), Em(17, 9, 50));
        var longe = Reuniao("NTM <> SplitC - Alinhamento", Em(17, 10, 6), Em(17, 10, 10));
        var outroTema = Reuniao("NTM <> SplitC - Daily", Em(16, 15, 0), Em(16, 15, 20));
        var cadastro = new CadastroDeEscritorio("2200", "NTM", [], []);

        var linha = Assert.Single(RelatorioDeEscritorios.Montar(
            [primeira, segunda, depois, longe, outroTema],
            Em(17, 18, 0),
            [cadastro]));

        Assert.Equal("NTM", linha.Rotulo);
        Assert.Equal(2, linha.Temas.Count);
        var alinhamento = linha.Temas[0];
        Assert.Equal("Alinhamento", alinhamento.Tema);
        Assert.Equal(TimeSpan.FromMinutes(70), alinhamento.Duracao);
        var dia = Assert.Single(alinhamento.Dias);
        Assert.Equal("ter. 17/11", dia.Rotulo);
        var ocorrencia = Assert.Single(dia.Ocorrencias);
        Assert.Equal(Em(17, 9, 0), ocorrencia.Inicio);
        Assert.Equal(Em(17, 10, 10), ocorrencia.Fim);
        Assert.Equal(TimeSpan.FromMinutes(70), ocorrencia.Duracao);
        Assert.Equal("Daily", linha.Temas[1].Tema);
        Assert.Equal("seg. 16/11", Assert.Single(linha.Temas[1].Dias).Rotulo);
    }

    [Fact]
    public void Meet_sem_tema_entra_no_escritorio_enquanto_esta_aberto()
    {
        var pedaco = Reuniao("Meet: vct <> splitc - Google Chrome", Em(17, 14, 0), Em(17, 14, 0).AddSeconds(8));
        var aberto = IntervaloDeEsforco.Abrir(
            "Google Chrome",
            "Meet: vct <> splitc - Google Chrome",
            "Meet: vct <> splitc",
            "",
            false,
            Em(17, 14, 0).AddSeconds(10));
        var cadastro = new CadastroDeEscritorio("1905", "VCT", [], []);

        var linha = Assert.Single(RelatorioDeEscritorios.Montar(
            [pedaco, aberto],
            Em(17, 14, 0).AddSeconds(40),
            [cadastro]));

        Assert.Equal("VCT", linha.Rotulo);
        var tema = Assert.Single(linha.Temas);
        Assert.Equal("Sem tema", tema.Tema);
        var ocorrencia = Assert.Single(Assert.Single(tema.Dias).Ocorrencias);
        Assert.Null(ocorrencia.Fim);
        Assert.Equal(TimeSpan.FromSeconds(40), ocorrencia.Duracao);
    }

    [Fact]
    public void Variacao_do_nome_liga_a_reuniao_ao_id()
    {
        var reuniao = Reuniao("NTM Sul <> SplitC - Fechamento", Em(17, 14, 0), Em(17, 14, 12));
        var cadastro = new CadastroDeEscritorio("2200", "NTM", ["NTM Sul"], []);

        var linha = Assert.Single(RelatorioDeEscritorios.Montar([reuniao], Em(17, 15, 0), [cadastro]));

        Assert.Equal("2200", linha.Id);
        Assert.Equal(TimeSpan.FromMinutes(12), linha.DuracaoReunioes);
        Assert.Equal("Fechamento", Assert.Single(linha.Temas).Tema);
    }

    [Fact]
    public void Nome_diferente_fica_sem_escritorio_ate_virar_variacao()
    {
        var reuniao = Reuniao("Outro nome <> SplitC - Tema", Em(17, 14, 0), Em(17, 14, 5));
        var cadastro = new CadastroDeEscritorio("2200", "NTM", [], []);

        var linhas = RelatorioDeEscritorios.Montar([reuniao], Em(17, 15, 0), [cadastro]);

        Assert.Equal("Outro nome", linhas[0].Rotulo);
        Assert.False(linhas[0].Editavel);
        Assert.Equal("NTM", linhas[1].Rotulo);
        Assert.Equal(TimeSpan.Zero, linhas[1].DuracaoReunioes);
    }

    [Fact]
    public void Plano_visto_no_historico_continua_na_ficha()
    {
        var ontem = Painel("2200", Em(16, 9, 0), Em(16, 9, 30));
        var cadastro = new CadastroDeEscritorio("2200", "NTM", [], [new NomeDePlano("17927", "Comissão")]);

        var linha = Assert.Single(RelatorioDeEscritorios.Montar([], Em(17, 12, 0), [cadastro], [ontem]));

        Assert.Equal(TimeSpan.Zero, linha.DuracaoPainel);
        var plano = Assert.Single(linha.Planos);
        Assert.Equal("17927", plano.Plano);
        Assert.Equal("Comissão", plano.Nome);
    }

    [Fact]
    public void Chatwoot_entra_no_escritorio_sem_mexer_no_painel()
    {
        var chat = Chat("1905", Em(17, 13, 40), Em(17, 14, 10));
        var durante = Chat("1905", Em(17, 14, 10), Em(17, 14, 40));
        var reuniao = Reuniao("VCT <> SplitC - Alinhamento", Em(17, 14, 0), Em(17, 15, 0));
        var cadastro = new CadastroDeEscritorio("1905", "Concha y Toro", ["VCT"], []);

        var linha = Assert.Single(
            RelatorioDeEscritorios.Montar([chat, durante, reuniao], Em(17, 16, 0), [cadastro]),
            item => item.Id == "1905");

        Assert.Equal(TimeSpan.FromMinutes(20), linha.DuracaoChat);
        Assert.Equal(TimeSpan.FromMinutes(40), linha.DuracaoChatNaReuniao);
        Assert.Equal(TimeSpan.Zero, linha.DuracaoPainel);
        Assert.Equal(TimeSpan.FromMinutes(60), linha.DuracaoReunioes);
    }

    [Fact]
    public void Chat_de_outro_escritorio_continua_inteiro_durante_a_reuniao()
    {
        var chat = Chat("1759", Em(17, 14, 10), Em(17, 14, 40));
        var reuniao = Reuniao("VCT <> SplitC - Alinhamento", Em(17, 14, 0), Em(17, 15, 0));
        var cadastros = new[]
        {
            new CadastroDeEscritorio("1905", "Concha y Toro", ["VCT"], []),
            new CadastroDeEscritorio("1759", "Straumann", [], []),
        };

        var linhas = RelatorioDeEscritorios.Montar([chat, reuniao], Em(17, 16, 0), cadastros);
        var straumann = Assert.Single(linhas, item => item.Id == "1759");
        var vct = Assert.Single(linhas, item => item.Id == "1905");

        Assert.Equal(TimeSpan.FromMinutes(30), straumann.DuracaoChat);
        Assert.Equal(TimeSpan.Zero, straumann.DuracaoChatNaReuniao);
        Assert.Equal(TimeSpan.Zero, vct.DuracaoChat);
        Assert.Equal(TimeSpan.FromMinutes(60), vct.DuracaoReunioes);
    }

    private static IntervaloDeEsforco Chat(string escritorio, DateTimeOffset inicio, DateTimeOffset fim)
    {
        var intervalo = IntervaloDeEsforco.Abrir(
            "Chatwoot",
            "Chatwoot - Google Chrome",
            "SplitC <> Concha y Toro - VCT",
            escritorio,
            false,
            inicio);
        intervalo.Fechar(fim);
        return intervalo;
    }

    private static IntervaloDeEsforco Painel(string escritorio, DateTimeOffset inicio, DateTimeOffset fim)
    {
        var intervalo = IntervaloDeEsforco.AbrirPainel("SplitC", Local(escritorio), false, inicio);
        intervalo.Fechar(fim);
        return intervalo;
    }

    private static IntervaloDeEsforco Reuniao(string titulo, DateTimeOffset inicio, DateTimeOffset fim)
    {
        var intervalo = IntervaloDeEsforco.Abrir("Google Chrome", titulo, titulo, "", false, inicio);
        intervalo.Fechar(fim);
        return intervalo;
    }

    private static LocalDoPainel Local(string escritorio) =>
        new(escritorio, "17927", "49797", "Fontes", "862247", TipoDeItem.Aba);
}
