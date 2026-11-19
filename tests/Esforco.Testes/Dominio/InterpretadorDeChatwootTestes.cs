using Esforco.Dominio;

namespace Esforco.Testes.Dominio;

public class InterpretadorDeChatwootTestes
{
    [Fact]
    public void Aba_do_chatwoot_e_a_conversa_aberta()
    {
        Assert.True(InterpretadorDeChatwoot.EhAba("Chatwoot - Google Chrome", ""));
        Assert.True(InterpretadorDeChatwoot.EhAba(
            "Inbox",
            "https://ticketing.splitc.com.br/app/accounts/1/inbox-view/conversation/22736"));
        Assert.False(InterpretadorDeChatwoot.EhAba("Gmail - Google Chrome", "https://mail.google.com/mail"));
        Assert.False(InterpretadorDeChatwoot.SaiuDaConversa(
            "https://ticketing.splitc.com.br/app/accounts/1/inbox-view/conversation/22736"));
        Assert.True(InterpretadorDeChatwoot.SaiuDaConversa(
            "https://ticketing.splitc.com.br/app/accounts/1/inbox-view"));
        Assert.False(InterpretadorDeChatwoot.SaiuDaConversa(""));
    }

    [Fact]
    public void Titulo_do_chat_ignora_os_botoes_do_cabecalho()
    {
        var titulo = InterpretadorDeChatwoot.Titulo(
        [
            "Resolver",
            "SplitC <> Concha y Toro - VCT",
            "#22736",
            "Whatsapp API",
            "SplitC <> Concha y Toro - VCT",
            "Oi Vic, caiu sim",
        ]);

        Assert.Equal("SplitC <> Concha y Toro - VCT", titulo);
    }

    [Fact]
    public void Titulo_com_hifen_ignora_etiquetas_do_contato()
    {
        var titulo = InterpretadorDeChatwoot.Titulo(
        [
            "Resolver",
            "#22964",
            "Whatsapp API",
            "•",
            "1_caminhao",
            "Indisponível",
            "Selecione o valor",
            "Teams",
            ")",
            "Em resposta a:",
            "George Junckes (554791399015)",
            "SplitC - Straumann",
        ]);

        Assert.Equal("SplitC - Straumann", titulo);
        Assert.Equal("", InterpretadorDeChatwoot.Titulo(["1_caminhao", "Indisponível", "Teams", ")"]));
    }

    [Fact]
    public void Nome_ou_variacao_inteiros_escolhem_o_escritorio()
    {
        var nomes = new NomesDePainel(
        [
            new CadastroDeEscritorio("1905", "Concha y Toro", ["VCT"], []),
            new CadastroDeEscritorio("1759", "Straumann", ["Straumann LATAM"], []),
        ]);

        Assert.Equal("1905", nomes.IdNoTexto("SplitC <> Concha y Toro - VCT"));
        Assert.Equal("1759", nomes.IdNoTexto("SplitC <> Straumann LATAM"));
        Assert.Equal("1759", nomes.IdNoTexto("SplitC - Straumann"));
        Assert.Null(nomes.IdNoTexto("SplitC <> VCTORE"));
        Assert.Null(nomes.IdNoTexto(""));
    }

    [Fact]
    public void O_trecho_mais_longo_vence()
    {
        var nomes = new NomesDePainel(
        [
            new CadastroDeEscritorio("1905", "Concha y Toro", [], []),
            new CadastroDeEscritorio("2000", "Outro", ["VCT"], []),
        ]);

        Assert.Equal("1905", nomes.IdNoTexto("SplitC <> Concha y Toro - VCT"));
    }
}
