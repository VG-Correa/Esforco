using Esforco.Dominio;
using static Esforco.Testes.Apoio.Vinculos;

namespace Esforco.Testes.Dominio;

public class RelatorioComVinculosTestes
{
    private static readonly CadastroDeEscritorio Ntm = new("2200", "NTM", [], []);
    private static readonly Categoria Contratos = new(1, "Contratos");

    [Fact]
    public void Trabalho_vinculado_entra_em_outros_trabalhos_e_no_total()
    {
        var painel = Painel("2200", Em(17, 9, 0), Em(17, 9, 10));
        var word = Janela("Word", "Contrato NTM", Em(17, 10, 0), Em(17, 10, 30));
        var excel = Janela("Excel", "Custos", Em(17, 11, 0), Em(17, 11, 15));
        var solto = Janela("Bloco de Notas", "Rascunho", Em(17, 12, 0), Em(17, 12, 40));
        var vinculos = Contexto(
        [
            Novo(1, "2200", AlcanceDoVinculo.Programa, "Word", categoria: 1),
            Novo(2, "2200", AlcanceDoVinculo.Programa, "Excel"),
        ],
        [Contratos]);

        var linhas = RelatorioDeEscritorios.Montar([painel, word, excel, solto], Em(17, 13, 0), [Ntm], null, vinculos);

        var linha = Assert.Single(linhas);
        Assert.Equal(TimeSpan.FromMinutes(10), linha.DuracaoPainel);
        Assert.Equal(TimeSpan.FromMinutes(45), linha.DuracaoOutros);
        Assert.Equal(TimeSpan.Zero, linha.DuracaoOutrosNaReuniao);
        Assert.Equal(TimeSpan.FromMinutes(55), linha.DuracaoContada);
        Assert.Equal(
            [new OutroTrabalho("Contratos", TimeSpan.FromMinutes(30), TimeSpan.Zero), new OutroTrabalho("Sem categoria", TimeSpan.FromMinutes(15), TimeSpan.Zero)],
            linha.Outros);
    }

    [Fact]
    public void Sem_vinculo_o_relatorio_fica_como_era()
    {
        var painel = Painel("2200", Em(17, 9, 0), Em(17, 9, 10));
        var word = Janela("Word", "Contrato NTM", Em(17, 10, 0), Em(17, 10, 30));

        var antes = Assert.Single(RelatorioDeEscritorios.Montar([painel, word], Em(17, 13, 0), [Ntm]));
        var depois = Assert.Single(RelatorioDeEscritorios.Montar([painel, word], Em(17, 13, 0), [Ntm], null, Contexto([])));

        Assert.Equal(TimeSpan.Zero, antes.DuracaoOutros);
        Assert.Null(antes.Outros);
        Assert.Equal(antes.DuracaoContada, depois.DuracaoContada);
        Assert.Equal(TimeSpan.FromMinutes(10), depois.DuracaoContada);
    }

    [Fact]
    public void Trabalho_vinculado_durante_a_reuniao_do_mesmo_escritorio_fica_azul_e_fora_do_total()
    {
        var reuniao = Reuniao("NTM <> SplitC - Alinhamento", Em(17, 14, 0), Em(17, 15, 0));
        var word = Janela("Word", "Contrato NTM", Em(17, 13, 50), Em(17, 14, 20));
        var excel = Janela("Excel", "Custos", Em(17, 15, 0), Em(17, 15, 30));
        var vinculos = Contexto(
        [
            Novo(1, "2200", AlcanceDoVinculo.Programa, "Word", categoria: 1),
            Novo(2, "2200", AlcanceDoVinculo.Programa, "Excel"),
        ],
        [Contratos]);

        var linha = Assert.Single(RelatorioDeEscritorios.Montar([reuniao, word, excel], Em(17, 16, 0), [Ntm], null, vinculos));

        Assert.Equal(TimeSpan.FromMinutes(60), linha.DuracaoReunioes);
        Assert.Equal(TimeSpan.FromMinutes(40), linha.DuracaoOutros);
        Assert.Equal(TimeSpan.FromMinutes(20), linha.DuracaoOutrosNaReuniao);
        Assert.Equal(TimeSpan.FromMinutes(100), linha.DuracaoContada);
        var contratos = Assert.Single(linha.Outros!, item => item.Categoria == "Contratos");
        Assert.Equal(TimeSpan.FromMinutes(10), contratos.Duracao);
        Assert.Equal(TimeSpan.FromMinutes(20), contratos.DuracaoNaReuniao);
    }

    [Fact]
    public void Reuniao_de_outro_escritorio_nao_tira_o_trabalho_vinculado_do_total()
    {
        var reuniao = Reuniao("VCT <> SplitC - Alinhamento", Em(17, 14, 0), Em(17, 15, 0));
        var word = Janela("Word", "Contrato NTM", Em(17, 14, 10), Em(17, 14, 40));
        var vinculos = Contexto([Novo(1, "2200", AlcanceDoVinculo.Programa, "Word")]);
        var vct = new CadastroDeEscritorio("1905", "VCT", [], []);

        var linhas = RelatorioDeEscritorios.Montar([reuniao, word], Em(17, 16, 0), [Ntm, vct], null, vinculos);

        var ntm = Assert.Single(linhas, linha => linha.Id == "2200");
        Assert.Equal(TimeSpan.FromMinutes(30), ntm.DuracaoOutros);
        Assert.Equal(TimeSpan.Zero, ntm.DuracaoOutrosNaReuniao);
    }

    [Fact]
    public void Marcacao_pinta_de_azul_o_registro_vinculado_que_cruza_a_reuniao()
    {
        var nomes = new NomesDePainel([Ntm]);
        var reuniao = Reuniao("NTM <> SplitC - Alinhamento", Em(17, 14, 0), Em(17, 15, 0));
        var dentro = Janela("Word", "Contrato NTM", Em(17, 14, 10), Em(17, 14, 20));
        var fora = Janela("Word", "Contrato NTM", Em(17, 16, 0), Em(17, 16, 20));
        var solto = Janela("Excel", "Custos", Em(17, 14, 20), Em(17, 14, 30));
        IntervaloDeEsforco[] todos = [reuniao, dentro, fora, solto];
        var vinculos = Contexto([Novo(1, "2200", AlcanceDoVinculo.Programa, "Word")]);
        var agora = Em(17, 17, 0);

        PainelDuranteReuniao.Marcar(todos, nomes, agora, AplicadorDeVinculos.Aplicar(todos, vinculos, nomes, agora));

        Assert.True(dentro.DuranteReuniao);
        Assert.Equal("NTM", dentro.EscritorioVinculado);
        Assert.False(fora.DuranteReuniao);
        Assert.Equal("NTM", fora.EscritorioVinculado);
        Assert.False(solto.DuranteReuniao);
        Assert.Equal("", solto.EscritorioVinculado);
    }

    [Fact]
    public void Vinculo_temporario_so_conta_o_trecho_em_que_valia()
    {
        var word = Janela("Word", "Contrato NTM", Em(17, 9, 0), Em(17, 11, 0));
        var vinculos = Contexto(
        [
            Novo(
                1,
                "2200",
                AlcanceDoVinculo.Programa,
                "Word",
                validade: ValidadeDoVinculo.Prazo,
                inicio: Em(17, 9, 30),
                duracao: TimeSpan.FromHours(1)),
        ]);

        var linha = Assert.Single(RelatorioDeEscritorios.Montar([word], Em(17, 12, 0), [Ntm], null, vinculos));

        Assert.Equal(TimeSpan.FromHours(1), linha.DuracaoOutros);
    }

    [Fact]
    public void Reuniao_sem_dono_vinculada_sai_das_reunioes_soltas_e_entra_em_outros_trabalhos()
    {
        var reuniao = Reuniao("Fulano <> SplitC - Conversa", Em(17, 14, 0), Em(17, 15, 0));
        var vinculos = Contexto([Novo(1, "2200", AlcanceDoVinculo.Janela, "Google Chrome", reuniao.Titulo)]);

        var linhas = RelatorioDeEscritorios.Montar([reuniao], Em(17, 16, 0), [Ntm], null, vinculos);

        var linha = Assert.Single(linhas);
        Assert.Equal("2200", linha.Id);
        Assert.Equal(TimeSpan.FromHours(1), linha.DuracaoOutros);
        Assert.Equal(TimeSpan.Zero, linha.DuracaoReunioes);
    }
}
