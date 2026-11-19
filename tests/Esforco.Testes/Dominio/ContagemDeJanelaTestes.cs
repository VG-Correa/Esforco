using Esforco.Dominio;
using Esforco.Testes.Apoio;

namespace Esforco.Testes.Dominio;

public class ContagemDeJanelaTestes
{
    private const string Reuniao = "Meet: vct <> splitc - Google Chrome";
    private static readonly DateTimeOffset Agora = Vinculos.Em(19, 14, 0);

    [Fact]
    public void Sem_regra_a_reuniao_conta_aberta_e_o_resto_so_em_foco()
    {
        Assert.Equal(ModoDeContagem.EnquantoAberta, ContagemDaJanela.Achar("Google Chrome", Reuniao, []).Modo);
        Assert.Equal(ModoDeContagem.SoEmFoco, ContagemDaJanela.Achar("Google Chrome", "Gmail - Google Chrome", []).Modo);
        Assert.Equal(ModoDeContagem.SoEmFoco, ContagemDaJanela.Achar("Discord", "amigos - Discord", []).Modo);
        Assert.Null(ContagemDaJanela.Achar("Discord", "amigos - Discord", []).Regra);
    }

    [Fact]
    public void Regra_da_janela_vale_mais_que_a_do_programa()
    {
        ContagemDeJanela[] regras =
        [
            Regra(1, AlcanceDaContagem.Programa, "Discord", "", ModoDeContagem.EnquantoAberta),
            Regra(2, AlcanceDaContagem.Janela, "Discord", "amigos - Discord", ModoDeContagem.SoEmFoco),
        ];

        var daJanela = ContagemDaJanela.Achar("discord", " Amigos - Discord ", regras);
        var doPrograma = ContagemDaJanela.Achar("Discord", "trabalho - Discord", regras);

        Assert.Equal(ModoDeContagem.SoEmFoco, daJanela.Modo);
        Assert.Equal(2, daJanela.Regra!.Id);
        Assert.Equal(ModoDeContagem.EnquantoAberta, doPrograma.Modo);
        Assert.Equal(1, doPrograma.Regra!.Id);
        Assert.Null(ContagemDaJanela.Achar("Word", "amigos - Discord", regras).Regra);
    }

    [Fact]
    public void Regra_da_reuniao_vale_para_a_aba_e_para_a_janelinha_flutuante_do_meet()
    {
        ContagemDeJanela[] regras = [Regra(1, AlcanceDaContagem.Janela, "Google Chrome", Reuniao, ModoDeContagem.SoEmFoco)];

        Assert.Equal(ModoDeContagem.SoEmFoco, ContagemDaJanela.Achar("Google Chrome", "Meet: vct <> splitc", regras).Modo);
        Assert.Equal(
            ModoDeContagem.EnquantoAberta,
            ContagemDaJanela.Achar("Google Chrome", "Meet: Galena <> SplitC - Google Chrome", regras).Modo);
    }

    [Fact]
    public void Salvar_a_escolha_que_ja_valia_nao_grava_nada()
    {
        Assert.Equal(
            AcaoDeContagem.Nada,
            ContagemDaJanela.Decidir("Discord", "amigos - Discord", AlcanceDaContagem.Janela, ModoDeContagem.SoEmFoco, [], Agora).Acao);
        Assert.Equal(
            AcaoDeContagem.Nada,
            ContagemDaJanela.Decidir("Google Chrome", Reuniao, AlcanceDaContagem.Janela, ModoDeContagem.EnquantoAberta, [], Agora).Acao);

        ContagemDeJanela[] regras = [Regra(1, AlcanceDaContagem.Programa, "Discord", "", ModoDeContagem.EnquantoAberta)];
        Assert.Equal(
            AcaoDeContagem.Nada,
            ContagemDaJanela.Decidir("Discord", "amigos - Discord", AlcanceDaContagem.Programa, ModoDeContagem.EnquantoAberta, regras, Agora).Acao);
        Assert.Equal(
            AcaoDeContagem.Nada,
            ContagemDaJanela.Decidir("Discord", "amigos - Discord", AlcanceDaContagem.Janela, ModoDeContagem.EnquantoAberta, regras, Agora).Acao);
    }

    [Fact]
    public void Escolha_nova_grava_a_regra_no_alcance_escolhido()
    {
        var janela = ContagemDaJanela.Decidir(
            " Discord ",
            " amigos - Discord ",
            AlcanceDaContagem.Janela,
            ModoDeContagem.EnquantoAberta,
            [],
            Agora);
        var programa = ContagemDaJanela.Decidir(
            "Discord",
            "amigos - Discord",
            AlcanceDaContagem.Programa,
            ModoDeContagem.EnquantoAberta,
            [],
            Agora);

        Assert.Equal(AcaoDeContagem.Gravar, janela.Acao);
        Assert.Equal(new ContagemDeJanela(0, AlcanceDaContagem.Janela, "Discord", "amigos - Discord", ModoDeContagem.EnquantoAberta, Agora), janela.Regra);
        Assert.Equal(AcaoDeContagem.Gravar, programa.Acao);
        Assert.Equal(new ContagemDeJanela(0, AlcanceDaContagem.Programa, "Discord", "", ModoDeContagem.EnquantoAberta, Agora), programa.Regra);
    }

    [Fact]
    public void Trocar_a_escolha_altera_a_mesma_regra()
    {
        ContagemDeJanela[] regras = [Regra(7, AlcanceDaContagem.Janela, "Discord", "amigos - Discord", ModoDeContagem.EnquantoAberta)];

        var plano = ContagemDaJanela.Decidir("Discord", "amigos - Discord", AlcanceDaContagem.Janela, ModoDeContagem.SoEmFoco, regras, Agora);

        Assert.Equal(AcaoDeContagem.Gravar, plano.Acao);
        Assert.Equal(7, plano.Regra!.Id);
        Assert.Equal(ModoDeContagem.SoEmFoco, plano.Regra.Modo);
    }

    [Fact]
    public void So_em_foco_no_programa_inteiro_fora_de_reuniao_tira_a_regra_do_programa()
    {
        ContagemDeJanela[] regras = [Regra(3, AlcanceDaContagem.Programa, "Google Chrome", "", ModoDeContagem.EnquantoAberta)];

        var plano = ContagemDaJanela.Decidir("Google Chrome", "Gmail - Google Chrome", AlcanceDaContagem.Programa, ModoDeContagem.SoEmFoco, regras, Agora);
        var semRegra = ContagemDaJanela.Decidir("Google Chrome", "Gmail - Google Chrome", AlcanceDaContagem.Programa, ModoDeContagem.SoEmFoco, [], Agora);

        Assert.Equal(AcaoDeContagem.Remover, plano.Acao);
        Assert.Equal(3, plano.Regra!.Id);
        Assert.Equal(AcaoDeContagem.Nada, semRegra.Acao);
    }

    [Fact]
    public void So_em_foco_no_programa_inteiro_a_partir_da_reuniao_fica_gravado()
    {
        var plano = ContagemDaJanela.Decidir("Google Chrome", Reuniao, AlcanceDaContagem.Programa, ModoDeContagem.SoEmFoco, [], Agora);

        Assert.Equal(AcaoDeContagem.Gravar, plano.Acao);
        Assert.Equal(AlcanceDaContagem.Programa, plano.Regra!.Alcance);
        ContagemDeJanela[] regras = [plano.Regra with { Id = 1 }];
        Assert.Equal(ModoDeContagem.SoEmFoco, ContagemDaJanela.Achar("Google Chrome", Reuniao, regras).Modo);

        var semMexer = ContagemDaJanela.Decidir("Google Chrome", "Gmail - Google Chrome", AlcanceDaContagem.Programa, ModoDeContagem.SoEmFoco, regras, Agora);
        Assert.Equal(AcaoDeContagem.Nada, semMexer.Acao);
    }

    [Fact]
    public void Janela_sem_titulo_so_aceita_o_programa_inteiro()
    {
        Assert.Equal(
            AcaoDeContagem.Nada,
            ContagemDaJanela.Decidir("Bloco de Notas", "", AlcanceDaContagem.Janela, ModoDeContagem.EnquantoAberta, [], Agora).Acao);
        Assert.Equal(
            AcaoDeContagem.Gravar,
            ContagemDaJanela.Decidir("Bloco de Notas", "", AlcanceDaContagem.Programa, ModoDeContagem.EnquantoAberta, [], Agora).Acao);
    }

    [Fact]
    public void Formulario_abre_com_o_padrao_quando_nada_foi_salvo()
    {
        var comum = FormularioDeContagem.Montar("Discord", "amigos - Discord", []);
        var reuniao = FormularioDeContagem.Montar("Google Chrome", Reuniao, []);
        var semTitulo = FormularioDeContagem.Montar("Bloco de Notas", "", []);

        Assert.Equal(ModoDeContagem.SoEmFoco, comum.Modo);
        Assert.Equal(AlcanceDaContagem.Janela, comum.Alcance);
        Assert.False(comum.Salva);
        Assert.False(comum.Reuniao);
        Assert.Equal(ModoDeContagem.EnquantoAberta, reuniao.Modo);
        Assert.True(reuniao.Reuniao);
        Assert.False(reuniao.Salva);
        Assert.False(semTitulo.PodeJanela);
        Assert.Equal(AlcanceDaContagem.Programa, semTitulo.Alcance);
    }

    [Fact]
    public void Formulario_abre_com_o_que_esta_salvo_e_sabe_o_que_ha_em_cada_alcance()
    {
        ContagemDeJanela[] regras =
        [
            Regra(1, AlcanceDaContagem.Programa, "Discord", "", ModoDeContagem.EnquantoAberta),
            Regra(2, AlcanceDaContagem.Janela, "Discord", "amigos - Discord", ModoDeContagem.SoEmFoco),
        ];

        var daJanela = FormularioDeContagem.Montar("Discord", "amigos - Discord", regras);
        var doPrograma = FormularioDeContagem.Montar("Discord", "trabalho - Discord", regras);

        Assert.True(daJanela.Salva);
        Assert.Equal(ModoDeContagem.SoEmFoco, daJanela.Modo);
        Assert.Equal(AlcanceDaContagem.Janela, daJanela.Alcance);
        Assert.Equal(ModoDeContagem.EnquantoAberta, daJanela.ModoSalvoEm(AlcanceDaContagem.Programa));
        Assert.True(doPrograma.Salva);
        Assert.Equal(ModoDeContagem.EnquantoAberta, doPrograma.Modo);
        Assert.Equal(AlcanceDaContagem.Programa, doPrograma.Alcance);
        Assert.Null(doPrograma.ModoSalvoEm(AlcanceDaContagem.Janela));
    }

    private static ContagemDeJanela Regra(long id, AlcanceDaContagem alcance, string programa, string titulo, ModoDeContagem modo) =>
        new(id, alcance, programa, titulo, modo, Vinculos.Em(18, 9, 0));
}
