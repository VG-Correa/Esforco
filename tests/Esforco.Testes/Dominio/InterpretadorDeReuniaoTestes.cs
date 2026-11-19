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
}
