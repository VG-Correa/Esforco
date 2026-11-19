using Esforco.Dominio;

namespace Esforco.Testes.Dominio;

public class InterpretadorDeReuniaoTestes
{
    [Theory]
    [InlineData("NTM <> SplitC - Alinhamento semanal", "NTM", "Alinhamento semanal")]
    [InlineData("Meet - NTM <> SplitC - Alinhamento semanal - Google Chrome", "NTM", "Alinhamento semanal")]
    [InlineData("NTM Sul <> SplitC - Fechamento - Google Meet - Microsoft Edge", "NTM Sul", "Fechamento")]
    [InlineData("  NTM   <>   splitc  -  Daily  ", "NTM", "Daily")]
    [InlineData("Meet: vct <> splitc - Google Chrome", "vct", "Sem tema")]
    [InlineData("Meet: vct <> splitc", "vct", "Sem tema")]
    [InlineData("NTM <> SplitC", "NTM", "Sem tema")]
    public void Le_o_escritorio_e_o_tema(string titulo, string nome, string tema)
    {
        var lida = InterpretadorDeReuniao.Ler(titulo);

        Assert.NotNull(lida);
        Assert.Equal(nome, lida.Nome);
        Assert.Equal(tema, lida.Tema);
    }

    [Theory]
    [InlineData("Gmail - Google Chrome")]
    [InlineData("NTM <> Outra empresa - Tema")]
    [InlineData("<> SplitC - Tema")]
    public void Ignora_titulo_que_nao_e_reuniao_da_splitc(string titulo)
    {
        Assert.Null(InterpretadorDeReuniao.Ler(titulo));
    }

    [Theory]
    [InlineData("Meet: Straumann LATAM <> SplitC - Google Chrome")]
    [InlineData("Meet: vct <> splitc")]
    [InlineData("Meet - NTM <> SplitC - Alinhamento semanal - Google Chrome")]
    [InlineData("NTM Sul <> SplitC - Fechamento - Google Meet - Microsoft Edge")]
    [InlineData("NTM <> SplitC - Daily | Microsoft Teams - Google Chrome")]
    [InlineData("NTM <> SplitC - Daily - Zoom")]
    public void Titulo_de_programa_de_chamada_e_chamada(string titulo)
    {
        Assert.True(InterpretadorDeReuniao.EhChamada(titulo));
        Assert.Equal(ModoDeContagem.EnquantoAberta, ContagemDaJanela.Padrao(titulo));
    }

    [Theory]
    [InlineData("Serasa <> SplitC - Dashboard | Relatório da reunião | Triple Session - Google Chrome")]
    [InlineData("NTM <> SplitC - Alinhamento semanal")]
    [InlineData("Convite: NTM <> SplitC - Daily - E-mail de SplitC - Google Chrome")]
    [InlineData("NTM <> SplitC - Como usar o Meet - Documentos Google - Google Chrome")]
    [InlineData("Meet: mgs-jeuy-unz - Google Chrome")]
    [InlineData("Gmail - Google Chrome")]
    public void Pagina_que_so_cita_a_reuniao_nao_e_chamada_e_conta_so_em_foco(string titulo)
    {
        Assert.False(InterpretadorDeReuniao.EhChamada(titulo));
        Assert.Equal(ModoDeContagem.SoEmFoco, ContagemDaJanela.Padrao(titulo));
    }

    [Theory]
    [InlineData("Meet - Google Chrome", true)]
    [InlineData("Meet: mgs-jeuy-unz - Google Chrome", true)]
    [InlineData("Meet: vct <> splitc - Google Chrome", false)]
    [InlineData("Serasa <> SplitC - Dashboard | Relatório da reunião | Triple Session - Google Chrome", false)]
    [InlineData("Google Meet - Google Chrome", false)]
    [InlineData("Gmail - Google Chrome", false)]
    public void Tela_do_meet_sem_nome_e_so_a_que_ainda_nao_mostra_a_reuniao(string titulo, bool esperado)
    {
        Assert.Equal(esperado, InterpretadorDeReuniao.EhTelaDoMeetSemNome(titulo));
    }
}
