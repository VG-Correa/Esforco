using Esforco.Dominio;

namespace Esforco.Testes.Dominio;

public class TextoDeDuracaoTestes
{
    [Theory]
    [InlineData(0, 18, 40, "00:18:40")]
    [InlineData(0, 16, 50, "00:16:50")]
    [InlineData(0, 2, 44, "00:02:44")]
    [InlineData(0, 1, 21, "00:01:21")]
    [InlineData(0, 0, 8, "00:00:08")]
    [InlineData(0, 0, 25, "00:00:25")]
    [InlineData(0, 0, 1, "00:00:01")]
    [InlineData(1, 1, 1, "01:01:01")]
    public void Mostra_horas_minutos_e_segundos(int horas, int minutos, int segundos, string texto)
    {
        Assert.Equal(texto, TextoDeDuracao.De(new TimeSpan(horas, minutos, segundos)));
    }
}
