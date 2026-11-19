using Esforco.Dominio;

namespace Esforco.Testes.Dominio;

public class AbasDoNavegadorTestes
{
    private const string Reuniao = "Meet: Straumann LATAM <> SplitC - Google Chrome";

    [Theory]
    [InlineData("Meet: Straumann LATAM <> SplitC")]
    [InlineData("Uso da memória em Meet: Straumann LATAM <> SplitC: parte do grupo SplitC: 412 MB")]
    [InlineData("Uso elevado da memória em Meet: Straumann LATAM <> SplitC: 1,0 GB")]
    [InlineData("Meet: Straumann LATAM <> SplitC - Câmera ou microfone gravando")]
    [InlineData("Meet: Straumann LATAM <> SplitC – Áudio em reprodução")]
    [InlineData("meet: straumann latam <> splitc")]
    public void Aba_com_o_titulo_e_o_que_o_navegador_acrescenta_esta_aberta(string aba)
    {
        var janela = Chrome("Gmail - Google Chrome", aba, "Gmail");

        Assert.Equal(PresencaDeAba.Aberta, AbasDoNavegador.Procurar(Reuniao, [janela]));
    }

    [Fact]
    public void Nenhuma_aba_com_o_titulo_em_janelas_lidas_por_inteiro_esta_fechada()
    {
        var janelas = new[]
        {
            Chrome("Gmail - Google Chrome", "Uso da memória em Gmail: 300 MB", "Uso da memória em Chatwoot: parte do grupo SplitC: 331 MB"),
            Chrome("Airbyte | Sources - Google Chrome", "Uso da memória em Airbyte | Sources: 188 MB"),
            new JanelaAberta("Discord", "amigos - Discord", false),
        };

        Assert.Equal(PresencaDeAba.Fechada, AbasDoNavegador.Procurar(Reuniao, janelas.Where(janela => janela.Navegador)));
        Assert.Equal(PresencaDeAba.Fechada, AbasDoNavegador.Procurar(Reuniao, []));
    }

    [Fact]
    public void Janela_sem_as_abas_lidas_deixa_a_resposta_incerta()
    {
        var lida = Chrome("Gmail - Google Chrome", "Gmail");
        var semLeitura = new JanelaAberta("Google Chrome", "Agenda - Google Chrome", false, Navegador: true);

        Assert.Equal(PresencaDeAba.Incerta, AbasDoNavegador.Procurar(Reuniao, [lida, semLeitura]));
    }

    [Fact]
    public void Lista_que_nao_traz_a_aba_a_mostra_nao_e_tida_como_completa()
    {
        var incompleta = Chrome("Gmail - Google Chrome", "Agenda");
        var vazia = Chrome("Gmail - Google Chrome");

        Assert.False(AbasDoNavegador.Confiavel(incompleta));
        Assert.False(AbasDoNavegador.Confiavel(vazia));
        Assert.Equal(PresencaDeAba.Incerta, AbasDoNavegador.Procurar(Reuniao, [incompleta]));
        Assert.Equal(PresencaDeAba.Incerta, AbasDoNavegador.Procurar(Reuniao, [vazia]));
    }

    [Fact]
    public void Janela_de_aplicativo_do_navegador_sem_aba_nenhuma_nao_esconde_nada()
    {
        var flutuante = new JanelaAberta("Google Chrome", "Picture-in-picture", false, Navegador: true, Abas: []);

        Assert.True(AbasDoNavegador.Confiavel(flutuante));
        Assert.Equal(PresencaDeAba.Fechada, AbasDoNavegador.Procurar(Reuniao, [flutuante]));
    }

    [Theory]
    [InlineData("Uso da memória em Planos de cálculo - SplitC: 245 MB")]
    [InlineData("(1) Pagamentos - RV GERENTES - SplitC")]
    [InlineData("Uso da memória em Meet: vct <> SplitC: 400 MB")]
    [InlineData("SplitCloud")]
    public void Titulo_que_so_aparece_dentro_de_outro_nao_fecha_nem_confirma(string aba)
    {
        var janela = Chrome("Gmail - Google Chrome", aba, "Gmail");

        Assert.Equal(PresencaDeAba.Incerta, AbasDoNavegador.Procurar("SplitC - Google Chrome", [janela]));
    }

    [Fact]
    public void Aba_do_proprio_titulo_curto_e_achada_inteira()
    {
        var janela = Chrome("Gmail - Google Chrome", "Uso da memória em SplitC: 318 MB", "Gmail");

        Assert.Equal(PresencaDeAba.Aberta, AbasDoNavegador.Procurar("SplitC - Google Chrome", [janela]));
    }

    [Theory]
    [InlineData("Gmail - Google Chrome", "Gmail", true)]
    [InlineData("Gmail — Mozilla Firefox", "Gmail", true)]
    [InlineData("Gmail - Microsoft\u200B Edge", "Gmail", true)]
    [InlineData("Picture-in-picture", "Picture-in-picture", false)]
    [InlineData("", "", false)]
    public void Pagina_tira_o_nome_do_navegador_do_fim_do_titulo(string titulo, string pagina, bool temNavegador)
    {
        Assert.Equal((pagina, temNavegador), AbasDoNavegador.Pagina(titulo));
    }

    private static JanelaAberta Chrome(string titulo, params string[] abas) =>
        new("Google Chrome", titulo, false, Navegador: true, Abas: abas);
}
