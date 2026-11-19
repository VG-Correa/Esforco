using Esforco.Dominio;
using static Esforco.Testes.Apoio.Vinculos;

namespace Esforco.Testes.Dominio;

public class VinculoDaJanelaTestes
{
    private static readonly DateTimeOffset Agora = Em(17, 15, 0);

    [Fact]
    public void Janela_com_vinculo_valendo_devolve_esse_vinculo()
    {
        var vinculo = Novo(1, "2200", AlcanceDoVinculo.Janela, "Word", "Contrato - Word");

        var achado = Achar("Word", "Contrato - Word", "", [vinculo]);

        Assert.Equal(vinculo, achado!.Vinculo);
        Assert.False(achado.Terminou);
    }

    [Fact]
    public void Outra_janela_ou_outro_programa_nao_tem_vinculo()
    {
        var vinculo = Novo(1, "2200", AlcanceDoVinculo.Janela, "Word", "Contrato - Word");

        Assert.Null(Achar("Word", "Outro - Word", "", [vinculo]));
        Assert.Null(Achar("Excel", "Contrato - Word", "", [vinculo]));
        Assert.Null(Achar("Word", "Contrato - Word", "", []));
    }

    [Fact]
    public void Programa_inteiro_vale_para_qualquer_janela_dele()
    {
        var vinculo = Novo(1, "2200", AlcanceDoVinculo.Programa, "Excel");

        var achado = Achar("excel", "custos.xlsx - Excel", "", [vinculo]);

        Assert.Equal(1, achado!.Vinculo.Id);
    }

    [Fact]
    public void Aba_e_achada_pelo_endereco_mesmo_com_outro_titulo()
    {
        var vinculo = Novo(1, "2200", AlcanceDoVinculo.Aba, "Google Chrome", "Sala - Google Chrome", "app.gather.town/app/sala");

        var achado = Achar("Google Chrome", "Outro nome - Google Chrome", "https://app.gather.town/app/sala?token=1", [vinculo]);

        Assert.Equal(1, achado!.Vinculo.Id);
        Assert.False(achado.Terminou);
    }

    [Fact]
    public void Entre_dois_valendo_fica_o_mesmo_que_o_relatorio_aplica()
    {
        var programa = Novo(1, "1905", AlcanceDoVinculo.Programa, "Google Chrome");
        var aba = Novo(2, "2200", AlcanceDoVinculo.Aba, "Google Chrome", "Sala - Google Chrome");

        var achado = Achar("Google Chrome", "Sala - Google Chrome", "", [programa, aba]);

        Assert.Equal(2, achado!.Vinculo.Id);
    }

    [Fact]
    public void Prazo_de_um_minuto_que_ja_passou_volta_como_terminado()
    {
        var vinculo = Novo(
            1,
            "2200",
            AlcanceDoVinculo.Janela,
            "Word",
            "Contrato - Word",
            validade: ValidadeDoVinculo.Prazo,
            inicio: Em(17, 10, 0),
            duracao: TimeSpan.FromMinutes(1));

        var achado = Achar("Word", "Contrato - Word", "", [vinculo]);

        Assert.Equal(1, achado!.Vinculo.Id);
        Assert.True(achado.Terminou);
    }

    [Fact]
    public void Prazo_que_ainda_corre_volta_como_valendo()
    {
        var vinculo = Novo(
            1,
            "2200",
            AlcanceDoVinculo.Janela,
            "Word",
            "Contrato - Word",
            validade: ValidadeDoVinculo.Prazo,
            inicio: Em(17, 14, 50),
            duracao: TimeSpan.FromMinutes(30));

        var achado = Achar("Word", "Contrato - Word", "", [vinculo]);

        Assert.False(achado!.Terminou);
    }

    [Fact]
    public void Vinculo_valendo_vem_antes_de_um_terminado()
    {
        var antigo = Novo(
            1,
            "1905",
            AlcanceDoVinculo.Janela,
            "Word",
            "Contrato - Word",
            validade: ValidadeDoVinculo.Prazo,
            inicio: Em(17, 10, 0),
            duracao: TimeSpan.FromMinutes(1));
        var atual = Novo(2, "2200", AlcanceDoVinculo.Programa, "Word");

        var achado = Achar("Word", "Contrato - Word", "", [antigo, atual]);

        Assert.Equal(2, achado!.Vinculo.Id);
        Assert.False(achado.Terminou);
    }

    [Fact]
    public void Entre_terminados_fica_o_que_terminou_por_ultimo()
    {
        var primeiro = Novo(
            1,
            "1905",
            AlcanceDoVinculo.Programa,
            "Word",
            validade: ValidadeDoVinculo.Prazo,
            inicio: Em(17, 9, 0),
            duracao: TimeSpan.FromMinutes(10));
        var segundo = Novo(
            2,
            "2200",
            AlcanceDoVinculo.Programa,
            "Word",
            validade: ValidadeDoVinculo.Prazo,
            inicio: Em(17, 11, 0),
            duracao: TimeSpan.FromMinutes(10));

        var achado = Achar("Word", "Contrato - Word", "", [segundo, primeiro]);

        Assert.Equal(2, achado!.Vinculo.Id);
        Assert.True(achado.Terminou);
    }

    [Fact]
    public void Vinculo_encerrado_na_mao_volta_como_terminado()
    {
        var vinculo = Novo(1, "2200", AlcanceDoVinculo.Programa, "Word", encerradoEm: Em(17, 12, 0));

        var achado = Achar("Word", "Contrato - Word", "", [vinculo]);

        Assert.True(achado!.Terminou);
    }

    [Fact]
    public void Inatividade_segue_valendo_enquanto_a_janela_esta_em_uso_e_termina_depois_do_tempo_sem_voltar()
    {
        var vinculo = Novo(
            1,
            "2200",
            AlcanceDoVinculo.Programa,
            "Word",
            validade: ValidadeDoVinculo.Inatividade,
            inicio: Em(17, 10, 0),
            duracao: TimeSpan.FromMinutes(30));
        var emUso = Janela("Word", "Contrato", Em(17, 10, 0), null, id: 1);
        var largado = Janela("Word", "Contrato", Em(17, 10, 0), Em(17, 10, 20), id: 2);

        var valendo = VinculoDaJanela.Achar("Word", "Contrato - Word", "", Contexto([vinculo]), [emUso], Agora);
        var terminado = VinculoDaJanela.Achar("Word", "Contrato - Word", "", Contexto([vinculo]), [largado], Agora);

        Assert.False(valendo!.Terminou);
        Assert.True(terminado!.Terminou);
    }

    [Fact]
    public void Registro_marcado_sozinho_no_calendario_nao_e_o_vinculo_da_janela()
    {
        var registro = Novo(
            1,
            "2200",
            AlcanceDoVinculo.Janela,
            "Word",
            "Contrato - Word",
            validade: ValidadeDoVinculo.Prazo,
            inicio: Em(17, 14, 0),
            duracao: TimeSpan.FromHours(2),
            soRegistro: true);

        Assert.Null(Achar("Word", "Contrato - Word", "", [registro]));
    }

    [Fact]
    public void Vinculo_marcado_para_comecar_depois_tambem_e_mostrado()
    {
        var vinculo = Novo(1, "2200", AlcanceDoVinculo.Programa, "Word", inicio: Em(18, 9, 0));

        var achado = Achar("Word", "Contrato - Word", "", [vinculo]);

        Assert.Equal(1, achado!.Vinculo.Id);
        Assert.False(achado.Terminou);
    }

    private static VinculoDaJanela? Achar(string programa, string titulo, string endereco, VinculoDeEscritorio[] vinculos) =>
        VinculoDaJanela.Achar(programa, titulo, endereco, Contexto(vinculos), [], Agora);
}
