using Esforco.Dominio;

namespace Esforco.Testes.Dominio;

public class ContinuidadeDeReuniaoTestes
{
    private static readonly TimeSpan Zona = TimeSpan.FromHours(-3);

    private static DateTimeOffset Em(int hora, int minuto) =>
        new(2026, 11, 17, hora, minuto, 0, Zona);

    [Fact]
    public void Trechos_da_mesma_reuniao_viram_o_intervalo_inteiro()
    {
        var primeira = Reuniao(Em(13, 59), Em(14, 0));
        var segunda = Reuniao(Em(14, 1), Em(14, 2));
        var word = IntervaloDeEsforco.Abrir("Word", "notas.docx - Word", "notas.docx", "", false, Em(14, 0));
        word.Fechar(Em(14, 1));

        var unidos = ContinuidadeDeReuniao.Unir([primeira, word, segunda]);

        var reuniao = Assert.Single(unidos, item => item.Programa == "Google Chrome");
        Assert.Equal(Em(13, 59), reuniao.Inicio);
        Assert.Equal(Em(14, 2), reuniao.Fim);
        Assert.Equal(Em(14, 0), primeira.Fim);
        Assert.NotSame(primeira, reuniao);
        Assert.Same(word, Assert.Single(unidos, item => item.Programa == "Word"));
    }

    [Fact]
    public void Mesma_reuniao_na_ultima_hora_conta_o_tempo_do_meio()
    {
        var primeira = Reuniao(Em(10, 0), Em(10, 18));
        var segunda = Reuniao(Em(10, 40), Em(10, 41));

        var reuniao = Assert.Single(ContinuidadeDeReuniao.Unir([primeira, segunda]));

        Assert.Equal(Em(10, 0), reuniao.Inicio);
        Assert.Equal(Em(10, 41), reuniao.Fim);
        Assert.Equal(TimeSpan.FromMinutes(41), reuniao.DuracaoAte(Em(10, 41)));
        Assert.Equal(Em(10, 18), primeira.Fim);
    }

    [Fact]
    public void Folga_maior_que_uma_hora_deixa_duas_reunioes()
    {
        var primeira = Reuniao(Em(9, 0), Em(9, 10));
        var segunda = Reuniao(Em(10, 11), Em(10, 20));

        var reunioes = ContinuidadeDeReuniao.Unir([primeira, segunda])
            .OrderBy(item => item.Inicio)
            .ToList();

        Assert.Equal(2, reunioes.Count);
        Assert.Equal(Em(9, 10), reunioes[0].Fim);
        Assert.Equal(Em(10, 11), reunioes[1].Inicio);
    }

    [Fact]
    public void Retomado_nao_atravessa_a_folga()
    {
        var primeira = Reuniao(Em(9, 0), Em(9, 10));
        var segunda = Reuniao(Em(9, 12), Em(9, 20));
        segunda.MarcarRetomado();

        var reunioes = ContinuidadeDeReuniao.Unir([primeira, segunda])
            .OrderBy(item => item.Inicio)
            .ToList();

        Assert.Equal(2, reunioes.Count);
        Assert.Equal(TimeSpan.FromMinutes(10), reunioes[0].DuracaoAte(Em(9, 20)));
        Assert.Equal(TimeSpan.FromMinutes(8), reunioes[1].DuracaoAte(Em(9, 20)));
        Assert.True(reunioes[1].Retomado);
    }

    private static IntervaloDeEsforco Reuniao(DateTimeOffset inicio, DateTimeOffset fim)
    {
        var intervalo = IntervaloDeEsforco.Abrir(
            "Google Chrome",
            "Meet: vct <> splitc - Google Chrome",
            "Meet: vct <> splitc",
            "",
            false,
            inicio);
        intervalo.Fechar(fim);
        return intervalo;
    }
}
