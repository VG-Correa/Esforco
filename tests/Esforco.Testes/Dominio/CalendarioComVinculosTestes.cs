using Esforco.Dominio;
using static Esforco.Testes.Apoio.Vinculos;

namespace Esforco.Testes.Dominio;

public class CalendarioComVinculosTestes
{
    private static readonly NomesDePainel Nomes = new([new CadastroDeEscritorio("2200", "NTM", [], [])]);

    [Fact]
    public void Filtro_de_escritorios_mostra_o_registro_vinculado_com_simbolo_proprio_e_categoria()
    {
        var word = Janela("Word", "Contrato NTM", Em(17, 10, 0), Em(17, 10, 30));
        var solto = Janela("Excel", "Custos", Em(17, 11, 0), Em(17, 11, 30));
        var vinculos = Contexto([Novo(1, "2200", AlcanceDoVinculo.Programa, "Word", categoria: 1)], [new Categoria(1, "Contratos")]);

        var bloco = Assert.Single(Dia([word, solto], ModoDoCalendario.Escritorios, vinculos).Blocos);

        Assert.Equal("NTM", bloco.Titulo);
        Assert.Equal("Contratos · Contrato NTM", bloco.Subtitulo);
        Assert.Equal(CalendarioDeEsforco.TipoVinculo, bloco.Tipo);
        Assert.Contains("Contratos", bloco.Dica);
        Assert.Equal(10 * 60, bloco.InicioMinuto);
        Assert.Equal(10 * 60 + 30, bloco.FimMinuto);
    }

    [Fact]
    public void Sem_categoria_o_subtitulo_traz_so_o_que_foi_feito()
    {
        var word = Janela("Word", "Contrato NTM", Em(17, 10, 0), Em(17, 10, 30));
        var vinculos = Contexto([Novo(1, "2200", AlcanceDoVinculo.Programa, "Word")]);

        var bloco = Assert.Single(Dia([word], ModoDoCalendario.Escritorios, vinculos).Blocos);

        Assert.Equal("Contrato NTM", bloco.Subtitulo);
    }

    [Fact]
    public void Vinculo_junta_o_trabalho_com_o_painel_do_mesmo_escritorio()
    {
        var painel = Painel("2200", Em(17, 10, 0), Em(17, 10, 20));
        var word = Janela("Word", "Contrato NTM", Em(17, 10, 20), Em(17, 10, 40));
        var vinculos = Contexto([Novo(1, "2200", AlcanceDoVinculo.Programa, "Word")]);

        var bloco = Assert.Single(Dia([painel, word], ModoDoCalendario.Escritorios, vinculos).Blocos);

        Assert.Equal("NTM", bloco.Titulo);
        Assert.Equal(10 * 60, bloco.InicioMinuto);
        Assert.Equal(10 * 60 + 40, bloco.FimMinuto);
        Assert.Contains("Vínculo", bloco.Tipo);
        Assert.Contains("Painel", bloco.Tipo);
    }

    [Fact]
    public void Toda_faixa_diz_o_que_o_clique_vai_vincular()
    {
        var primeiro = Janela("Word", "Contrato NTM", Em(17, 10, 0), Em(17, 10, 10), id: 4);
        var segundo = Janela("Word", "Contrato NTM", Em(17, 10, 12), Em(17, 10, 40), id: 5);

        var bloco = Assert.Single(Dia([primeiro, segundo], ModoDoCalendario.Programas, null).Blocos);

        var alvo = Assert.IsType<AlvoDeVinculo>(bloco.Alvo);
        Assert.Equal("Word", alvo.Programa);
        Assert.Equal("Contrato NTM - Word", alvo.Titulo);
        Assert.Equal(Em(17, 10, 0), alvo.Inicio);
        Assert.Equal(Em(17, 10, 40), alvo.Fim);
        Assert.True(alvo.DoCalendario);
        Assert.False(alvo.Descoberto);
    }

    [Fact]
    public void Faixa_do_painel_avisa_que_o_escritorio_foi_descoberto_pelo_programa()
    {
        var painel = Painel("2200", Em(17, 10, 0), Em(17, 10, 20));

        var bloco = Assert.Single(Dia([painel], ModoDoCalendario.Escritorios, null).Blocos);

        Assert.True(bloco.Alvo!.Descoberto);
    }

    [Fact]
    public void Faixa_fundida_escolhe_a_janela_que_mais_durou_entre_as_que_aceitam_vinculo()
    {
        var curto = Janela("Google Chrome", "Gmail", Em(17, 10, 0), Em(17, 10, 5));
        var longo = Janela("Google Chrome", "Planilha NTM", Em(17, 10, 5), Em(17, 10, 40));

        var bloco = Assert.Single(Dia([curto, longo], ModoDoCalendario.Programas, null).Blocos);

        Assert.Equal("Planilha NTM - Google Chrome", bloco.Alvo!.Titulo);
        Assert.Equal(Em(17, 10, 0), bloco.Alvo.Inicio);
        Assert.Equal(Em(17, 10, 40), bloco.Alvo.Fim);
    }

    [Fact]
    public void Marcar_so_o_registro_da_faixa_cobre_o_periodo_inteiro_dela()
    {
        var primeiro = Janela("Word", "Contrato NTM", Em(17, 10, 0), Em(17, 10, 10));
        var segundo = Janela("Word", "Contrato NTM", Em(17, 10, 12), Em(17, 10, 40));
        var depois = Janela("Word", "Contrato NTM", Em(17, 15, 0), Em(17, 15, 30));
        IntervaloDeEsforco[] todos = [primeiro, segundo, depois];
        var alvo = Dia(todos, ModoDoCalendario.Programas, null).Blocos[0].Alvo!;
        var pedido = new PedidoDeVinculo("2200", null, AlcanceDoVinculo.Janela, alvo, ValidadeDoVinculo.Sempre, Em(17, 17, 0), null, SoRegistro: true);
        var vinculos = Contexto([VinculoDeEscritorio.De(pedido, null, Em(17, 17, 0), 1)]);

        var blocos = Dia(todos, ModoDoCalendario.Escritorios, vinculos).Blocos;
        var linha = Assert.Single(RelatorioDeEscritorios.Montar(todos, Em(17, 18, 0), null, null, vinculos));

        var bloco = Assert.Single(blocos);
        Assert.Equal(10 * 60, bloco.InicioMinuto);
        Assert.Equal(10 * 60 + 40, bloco.FimMinuto);
        Assert.Equal(TimeSpan.FromMinutes(38), linha.DuracaoOutros);
    }

    [Fact]
    public void Programas_e_telas_ganham_o_simbolo_de_vinculo_quando_o_registro_esta_vinculado()
    {
        var word = Janela("Word", "Contrato NTM", Em(17, 10, 0), Em(17, 10, 30));
        var excel = Janela("Excel", "Custos", Em(17, 11, 0), Em(17, 11, 30));
        var vinculos = Contexto([Novo(1, "2200", AlcanceDoVinculo.Programa, "Word")]);

        var blocos = Dia([word, excel], ModoDoCalendario.Programas, vinculos).Blocos;

        Assert.Equal(CalendarioDeEsforco.TipoVinculo, Assert.Single(blocos, bloco => bloco.Titulo == "Word").Tipo);
        Assert.Equal("", Assert.Single(blocos, bloco => bloco.Titulo == "Excel").Tipo);
    }

    [Fact]
    public void Faixa_vinculada_por_um_minuto_diz_qual_vinculo_o_clique_abre()
    {
        var word = Janela("Word", "Contrato NTM", Em(17, 10, 0), Em(17, 10, 30));
        var vinculos = Contexto([
            Novo(
                7,
                "2200",
                AlcanceDoVinculo.Janela,
                "Word",
                "Contrato NTM - Word",
                validade: ValidadeDoVinculo.Prazo,
                inicio: Em(17, 10, 10),
                duracao: TimeSpan.FromMinutes(1)),
        ]);

        var bloco = Assert.Single(Dia([word], ModoDoCalendario.Escritorios, vinculos).Blocos);

        Assert.Equal("NTM", bloco.Titulo);
        Assert.Equal(10 * 60 + 10, bloco.InicioMinuto);
        Assert.Equal(10 * 60 + 11, bloco.FimMinuto);
        Assert.Equal(7, bloco.Alvo!.VinculoId);
        Assert.Equal(Em(17, 10, 10), bloco.Alvo.Inicio);
        Assert.Equal(Em(17, 10, 11), bloco.Alvo.Fim);
    }

    [Fact]
    public void Faixa_sem_vinculo_nao_aponta_vinculo_nenhum()
    {
        var word = Janela("Word", "Contrato NTM", Em(17, 10, 0), Em(17, 10, 30));
        var vinculos = Contexto([Novo(1, "2200", AlcanceDoVinculo.Programa, "Excel")]);

        var bloco = Assert.Single(Dia([word], ModoDoCalendario.Programas, vinculos).Blocos);

        Assert.Null(bloco.Alvo!.VinculoId);
        Assert.Null(bloco.Alvo.CopiarDe);
    }

    [Fact]
    public void Faixa_fundida_abre_o_vinculo_mesmo_quando_o_pedaco_vinculado_e_o_menor()
    {
        var word = Janela("Word", "Contrato NTM", Em(17, 10, 0), Em(17, 10, 40));
        var vinculos = Contexto([
            Novo(
                7,
                "2200",
                AlcanceDoVinculo.Janela,
                "Word",
                "Contrato NTM - Word",
                validade: ValidadeDoVinculo.Prazo,
                inicio: Em(17, 10, 10),
                duracao: TimeSpan.FromMinutes(1)),
        ]);

        var bloco = Assert.Single(Dia([word], ModoDoCalendario.Programas, vinculos).Blocos);

        Assert.Equal(10 * 60, bloco.InicioMinuto);
        Assert.Equal(10 * 60 + 40, bloco.FimMinuto);
        Assert.Equal(7, bloco.Alvo!.VinculoId);
    }

    [Fact]
    public void Faixa_com_dois_vinculos_abre_o_que_cobriu_mais_tempo()
    {
        var word = Janela("Word", "Contrato NTM", Em(17, 10, 0), Em(17, 10, 40));
        var vinculos = Contexto([
            Novo(
                7,
                "2200",
                AlcanceDoVinculo.Janela,
                "Word",
                "Contrato NTM - Word",
                validade: ValidadeDoVinculo.Prazo,
                inicio: Em(17, 10, 0),
                duracao: TimeSpan.FromMinutes(5)),
            Novo(
                8,
                "2200",
                AlcanceDoVinculo.Janela,
                "Word",
                "Contrato NTM - Word",
                validade: ValidadeDoVinculo.Prazo,
                inicio: Em(17, 10, 5),
                duracao: TimeSpan.FromMinutes(35)),
        ]);

        var bloco = Assert.Single(Dia([word], ModoDoCalendario.Escritorios, vinculos).Blocos);

        Assert.Equal(8, bloco.Alvo!.VinculoId);
    }

    private static DiaDeCalendario Dia(
        IEnumerable<IntervaloDeEsforco> intervalos,
        ModoDoCalendario modo,
        ContextoDeVinculos? vinculos)
    {
        var quadro = CalendarioDeEsforco.Montar(
            intervalos,
            new DateTime(2026, 11, 17),
            new DateTime(2026, 11, 17),
            Em(17, 18, 0),
            modo,
            Nomes,
            false,
            false,
            null,
            vinculos);
        return Assert.Single(quadro.Dias);
    }
}
