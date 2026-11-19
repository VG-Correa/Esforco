using Esforco.Dominio;
using static Esforco.Testes.Apoio.Vinculos;

namespace Esforco.Testes.Dominio;

public class RegistroManualNasTelasTestes
{
    private static readonly CadastroDeEscritorio Ntm = new("2200", "NTM", [], []);
    private static readonly CadastroDeEscritorio Vct = new("3300", "VCT", [], []);
    private static readonly NomesDePainel Nomes = new([Ntm, Vct]);

    [Fact]
    public void Registro_manual_entra_em_outros_trabalhos_pela_categoria_e_no_total()
    {
        var comCategoria = Manual(1, "2200", Em(17, 10, 0), Em(17, 11, 0), "Suporte", "Ligação com o cliente");
        var semCategoria = Manual(2, "2200", Em(17, 13, 0), Em(17, 13, 20));

        var linha = Assert.Single(RelatorioDeEscritorios.Montar([comCategoria, semCategoria], Em(17, 18, 0), [Ntm]));

        Assert.Equal("2200", linha.Id);
        Assert.Equal(TimeSpan.FromMinutes(80), linha.DuracaoOutros);
        Assert.Equal(TimeSpan.Zero, linha.DuracaoOutrosNaReuniao);
        Assert.Equal(TimeSpan.FromMinutes(80), linha.DuracaoContada);
        Assert.Equal(
            [
                new OutroTrabalho("Suporte", TimeSpan.FromMinutes(60), TimeSpan.Zero),
                new OutroTrabalho("Sem categoria", TimeSpan.FromMinutes(20), TimeSpan.Zero),
            ],
            linha.Outros);
    }

    [Fact]
    public void Registro_manual_soma_com_o_vinculo_da_mesma_categoria()
    {
        var word = Janela("Word", "Contrato NTM", Em(17, 9, 0), Em(17, 9, 30));
        var manual = Manual(1, "2200", Em(17, 10, 0), Em(17, 10, 45), "Contratos");
        var vinculos = Contexto([Novo(1, "2200", AlcanceDoVinculo.Programa, "Word", categoria: 1)], [new Categoria(1, "Contratos")]);

        var linha = Assert.Single(RelatorioDeEscritorios.Montar([word, manual], Em(17, 18, 0), [Ntm], null, vinculos));

        Assert.Equal([new OutroTrabalho("Contratos", TimeSpan.FromMinutes(75), TimeSpan.Zero)], linha.Outros);
        Assert.Equal(TimeSpan.FromMinutes(75), linha.DuracaoContada);
    }

    [Fact]
    public void Painel_do_mesmo_escritorio_no_mesmo_horario_nao_soma_em_dobro()
    {
        var painel = Painel("2200", Em(17, 10, 0), Em(17, 10, 20));
        var manual = Manual(1, "2200", Em(17, 10, 0), Em(17, 11, 0), "Suporte");

        var linha = Assert.Single(RelatorioDeEscritorios.Montar([painel, manual], Em(17, 18, 0), [Ntm]));

        Assert.Equal(TimeSpan.FromMinutes(20), linha.DuracaoPainel);
        Assert.Equal(TimeSpan.FromMinutes(40), linha.DuracaoOutros);
        Assert.Equal(TimeSpan.FromMinutes(20), linha.DuracaoOutrosNaReuniao);
        Assert.Equal(TimeSpan.FromMinutes(60), linha.DuracaoContada);
    }

    [Fact]
    public void Reuniao_do_mesmo_escritorio_no_mesmo_horario_nao_soma_em_dobro()
    {
        var reuniao = Reuniao("NTM <> SplitC - Alinhamento", Em(17, 14, 0), Em(17, 15, 0));
        var manual = Manual(1, "2200", Em(17, 14, 30), Em(17, 15, 30));

        var linha = Assert.Single(RelatorioDeEscritorios.Montar([reuniao, manual], Em(17, 18, 0), [Ntm]));

        Assert.Equal(TimeSpan.FromMinutes(60), linha.DuracaoReunioes);
        Assert.Equal(TimeSpan.FromMinutes(30), linha.DuracaoOutros);
        Assert.Equal(TimeSpan.FromMinutes(30), linha.DuracaoOutrosNaReuniao);
        Assert.Equal(TimeSpan.FromMinutes(90), linha.DuracaoContada);
    }

    [Fact]
    public void Trabalho_vinculado_do_mesmo_escritorio_no_mesmo_horario_nao_soma_em_dobro()
    {
        var word = Janela("Word", "Contrato NTM", Em(17, 10, 0), Em(17, 10, 30));
        var manual = Manual(1, "2200", Em(17, 10, 15), Em(17, 11, 0));
        var vinculos = Contexto([Novo(1, "2200", AlcanceDoVinculo.Programa, "Word")]);

        var linha = Assert.Single(RelatorioDeEscritorios.Montar([word, manual], Em(17, 18, 0), [Ntm], null, vinculos));

        Assert.Equal(TimeSpan.FromMinutes(60), linha.DuracaoOutros);
        Assert.Equal(TimeSpan.FromMinutes(15), linha.DuracaoOutrosNaReuniao);
        Assert.Equal(TimeSpan.FromMinutes(60), linha.DuracaoContada);
    }

    [Fact]
    public void Dois_registros_manuais_do_mesmo_escritorio_no_mesmo_horario_contam_uma_vez_so()
    {
        var segundo = Manual(2, "2200", Em(17, 10, 30), Em(17, 11, 30), "Contratos");
        var primeiro = Manual(1, "2200", Em(17, 10, 0), Em(17, 11, 0), "Suporte");

        var linha = Assert.Single(RelatorioDeEscritorios.Montar([segundo, primeiro], Em(17, 18, 0), [Ntm]));

        Assert.Equal(TimeSpan.FromMinutes(90), linha.DuracaoOutros);
        Assert.Equal(TimeSpan.FromMinutes(30), linha.DuracaoOutrosNaReuniao);
        Assert.Equal(
            [
                new OutroTrabalho("Contratos", TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(30)),
                new OutroTrabalho("Suporte", TimeSpan.FromMinutes(60), TimeSpan.Zero),
            ],
            linha.Outros);
    }

    [Fact]
    public void Registros_de_escritorios_diferentes_no_mesmo_horario_contam_cada_um_o_seu()
    {
        var painel = Painel("2200", Em(17, 10, 0), Em(17, 10, 20));
        var manual = Manual(1, "3300", Em(17, 10, 0), Em(17, 11, 0));

        var linhas = RelatorioDeEscritorios.Montar([painel, manual], Em(17, 18, 0), [Ntm, Vct]);

        var ntm = Assert.Single(linhas, linha => linha.Id == "2200");
        var vct = Assert.Single(linhas, linha => linha.Id == "3300");
        Assert.Equal(TimeSpan.FromMinutes(20), ntm.DuracaoContada);
        Assert.Equal(TimeSpan.FromMinutes(60), vct.DuracaoContada);
        Assert.Equal(TimeSpan.Zero, vct.DuracaoOutrosNaReuniao);
    }

    [Fact]
    public void Registro_manual_cria_a_linha_de_um_escritorio_que_ainda_nao_tinha_nada()
    {
        var manual = Manual(1, "4400", Em(17, 10, 0), Em(17, 10, 30));

        var linha = Assert.Single(RelatorioDeEscritorios.Montar([manual], Em(17, 18, 0), [Ntm]), item => item.Id == "4400");

        Assert.Equal(TimeSpan.FromMinutes(30), linha.DuracaoContada);
    }

    [Fact]
    public void Registro_marca_em_azul_o_manual_que_coincide_com_outro_registro_do_mesmo_escritorio()
    {
        var painel = Painel("2200", Em(17, 10, 0), Em(17, 10, 20));
        var coincide = Manual(1, "2200", Em(17, 10, 10), Em(17, 11, 0));
        var livre = Manual(2, "2200", Em(17, 13, 0), Em(17, 13, 30));
        var deOutro = Manual(3, "3300", Em(17, 10, 0), Em(17, 10, 30));
        var repetido = Manual(4, "3300", Em(17, 10, 15), Em(17, 10, 45));
        IntervaloDeEsforco[] lista = [painel, coincide, livre, deOutro, repetido];
        var trechos = AplicadorDeVinculos.Aplicar(lista, null, Nomes, Em(17, 18, 0));

        PainelDuranteReuniao.Marcar(lista, Nomes, Em(17, 18, 0), trechos);

        Assert.True(coincide.DuranteReuniao);
        Assert.False(livre.DuranteReuniao);
        Assert.False(deOutro.DuranteReuniao);
        Assert.True(repetido.DuranteReuniao);
        Assert.False(painel.DuranteReuniao);
        Assert.Equal("NTM", coincide.EscritorioVinculado);
        Assert.Equal("VCT", deOutro.EscritorioVinculado);
    }

    [Fact]
    public void Calendario_mostra_o_registro_manual_com_tipo_proprio_e_sem_alvo_de_vinculo()
    {
        var manual = Manual(7, "2200", Em(17, 10, 0), Em(17, 10, 45), "Suporte", "Ligação com o cliente");

        var bloco = Assert.Single(Dia([manual], ModoDoCalendario.Escritorios).Blocos);

        Assert.Equal("NTM", bloco.Titulo);
        Assert.Equal("Suporte · Ligação com o cliente", bloco.Subtitulo);
        Assert.Equal(CalendarioDeEsforco.TipoManual, bloco.Tipo);
        Assert.Equal(7L, bloco.Manual);
        Assert.Null(bloco.Alvo);
        Assert.Equal("id:2200", bloco.Chave);
        Assert.Equal(10 * 60, bloco.InicioMinuto);
        Assert.Equal(10 * 60 + 45, bloco.FimMinuto);
        Assert.StartsWith("Manual\nNTM\n", bloco.Dica);
    }

    [Theory]
    [InlineData("", "", "Registro manual")]
    [InlineData("Suporte", "", "Suporte")]
    [InlineData("", "Ligação", "Ligação")]
    public void Subtitulo_do_registro_manual_usa_o_que_foi_preenchido(string categoria, string descricao, string esperado)
    {
        var manual = Manual(1, "2200", Em(17, 10, 0), Em(17, 10, 45), categoria, descricao);

        Assert.Equal(esperado, Assert.Single(Dia([manual], ModoDoCalendario.Escritorios).Blocos).Subtitulo);
    }

    [Fact]
    public void Registro_manual_fica_em_faixa_paralela_a_automatica_e_o_dia_nao_soma_em_dobro()
    {
        var painel = Painel("2200", Em(17, 10, 0), Em(17, 10, 20));
        var manual = Manual(1, "2200", Em(17, 10, 0), Em(17, 11, 0));

        var dia = Dia([painel, manual], ModoDoCalendario.Escritorios);

        Assert.Equal(2, dia.Blocos.Count);
        var automatico = Assert.Single(dia.Blocos, bloco => bloco.Manual is null);
        var lancado = Assert.Single(dia.Blocos, bloco => bloco.Manual == 1);
        Assert.NotEqual(automatico.Trilha, lancado.Trilha);
        Assert.Equal(automatico.Chave, lancado.Chave);
        Assert.NotNull(automatico.Alvo);
        Assert.Equal("Painel", automatico.Tipo);
        Assert.Equal("01:00:00", dia.Duracao);
        var linha = Assert.Single(dia.Linhas);
        Assert.Equal(60, linha.Minutos);
    }

    [Fact]
    public void Registro_manual_encostado_em_outra_faixa_nao_se_funde_com_ela()
    {
        var painel = Painel("2200", Em(17, 10, 0), Em(17, 10, 20));
        var primeiro = Manual(1, "2200", Em(17, 10, 20), Em(17, 10, 40));
        var segundo = Manual(2, "2200", Em(17, 10, 40), Em(17, 11, 0));

        var dia = Dia([painel, primeiro, segundo], ModoDoCalendario.Escritorios);

        Assert.Equal(3, dia.Blocos.Count);
        Assert.Equal(new long?[] { null, 1, 2 }, dia.Blocos.Select(bloco => bloco.Manual).ToArray());
        Assert.Equal("01:00:00", dia.Duracao);
    }

    [Fact]
    public void Registro_manual_de_outro_escritorio_no_mesmo_horario_soma_no_dia()
    {
        var painel = Painel("2200", Em(17, 10, 0), Em(17, 10, 20));
        var manual = Manual(1, "3300", Em(17, 10, 0), Em(17, 11, 0));

        var dia = Dia([painel, manual], ModoDoCalendario.Escritorios);

        Assert.Equal("01:20:00", dia.Duracao);
        Assert.Equal(2, dia.Linhas.Count);
    }

    [Fact]
    public void Registro_que_passa_da_meia_noite_aparece_nos_dois_dias_do_calendario()
    {
        var manual = Manual(1, "2200", Em(17, 23, 0), Em(18, 1, 0));

        var quadro = CalendarioDeEsforco.Montar(
            [manual],
            new DateTime(2026, 11, 17),
            new DateTime(2026, 11, 18),
            Em(18, 12, 0),
            ModoDoCalendario.Escritorios,
            Nomes,
            false,
            false);

        var antes = Assert.Single(quadro.Dias[0].Blocos);
        var depois = Assert.Single(quadro.Dias[1].Blocos);
        Assert.Equal((23 * 60, 24 * 60), (antes.InicioMinuto, antes.FimMinuto));
        Assert.Equal((0, 60), (depois.InicioMinuto, depois.FimMinuto));
        Assert.Equal(1L, antes.Manual);
        Assert.Equal(1L, depois.Manual);
    }

    [Fact]
    public void Nos_filtros_de_programas_e_de_telas_o_registro_manual_tambem_aparece()
    {
        var manual = Manual(1, "2200", Em(17, 10, 0), Em(17, 10, 45), "Suporte", "Ligação");
        var semDescricao = Manual(2, "2200", Em(17, 12, 0), Em(17, 12, 30));

        var programa = Assert.Single(Dia([manual], ModoDoCalendario.Programas).Blocos);
        var tela = Assert.Single(Dia([manual], ModoDoCalendario.Telas).Blocos);
        var telaSemDescricao = Assert.Single(Dia([semDescricao], ModoDoCalendario.Telas).Blocos);

        Assert.Equal("Registro manual", programa.Titulo);
        Assert.Equal("NTM · Suporte · Ligação", programa.Subtitulo);
        Assert.Equal(1L, programa.Manual);
        Assert.Equal("Ligação", tela.Titulo);
        Assert.Equal("Registro manual", tela.Subtitulo);
        Assert.Equal(CalendarioDeEsforco.TipoManual, tela.Tipo);
        Assert.Equal("NTM", telaSemDescricao.Titulo);
    }

    [Fact]
    public void Escritorio_oculto_some_do_calendario_de_escritorios_tambem_no_registro_manual()
    {
        var manual = Manual(1, "2200", Em(17, 10, 0), Em(17, 10, 45));

        var quadro = CalendarioDeEsforco.Montar(
            [manual],
            new DateTime(2026, 11, 17),
            new DateTime(2026, 11, 17),
            Em(17, 18, 0),
            ModoDoCalendario.Escritorios,
            Nomes,
            false,
            false,
            [new OcultacaoDeEscritorio(true, "2200")]);

        Assert.Empty(Assert.Single(quadro.Dias).Blocos);
    }

    private static DiaDeCalendario Dia(IEnumerable<IntervaloDeEsforco> intervalos, ModoDoCalendario modo)
    {
        var quadro = CalendarioDeEsforco.Montar(
            intervalos,
            new DateTime(2026, 11, 17),
            new DateTime(2026, 11, 17),
            Em(17, 18, 0),
            modo,
            Nomes,
            false,
            false);
        return Assert.Single(quadro.Dias);
    }
}
