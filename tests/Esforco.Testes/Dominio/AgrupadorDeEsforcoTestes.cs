using Esforco.Dominio;

namespace Esforco.Testes.Dominio;

public class AgrupadorDeEsforcoTestes
{
    private static DateTimeOffset Em(int hora, int minuto) =>
        new(2026, 11, 17, hora, minuto, 0, TimeSpan.FromHours(-3));

    [Fact]
    public void Lista_vazia_nao_gera_grupo()
    {
        var grupos = AgrupadorDeEsforco.Agrupar([], Em(12, 0));

        Assert.Empty(grupos);
    }

    [Fact]
    public void Agrupa_por_programa_e_dentro_por_contexto()
    {
        var word = Fechado("Word", "relatorio.docx", Em(9, 0), Em(9, 10));
        var chromeCaixa = Fechado("Chrome", "Caixa", Em(9, 10), Em(9, 25));
        var chromeOutra = Fechado("Chrome", "Agenda", Em(9, 30), Em(9, 40));
        var chromeCaixaDeNovo = IntervaloDeEsforco.Abrir("Chrome", "Caixa - Google Chrome", "Caixa", "", false, Em(10, 0));

        var grupos = AgrupadorDeEsforco.Agrupar(
            [word, chromeCaixa, chromeOutra, chromeCaixaDeNovo],
            Em(10, 3));

        var dia = grupos[0];
        Assert.Equal("ter. 17/11", dia.Rotulo);
        Assert.Equal(TimeSpan.FromMinutes(38), dia.DuracaoTotal);
        Assert.Equal("Chrome", dia.Filhos[0].Rotulo);
        Assert.Equal(TimeSpan.FromMinutes(28), dia.Filhos[0].DuracaoTotal);
        Assert.Equal("Caixa", dia.Filhos[0].Filhos[0].Rotulo);
        Assert.Equal(2, dia.Filhos[0].Filhos[0].Intervalos.Count);
        Assert.Equal(Em(10, 0), dia.Filhos[0].Filhos[0].Intervalos[0].Inicio);
        Assert.Equal("Agenda", dia.Filhos[0].Filhos[1].Rotulo);
        Assert.Equal("Word", dia.Filhos[1].Rotulo);
        Assert.Equal("relatorio.docx", dia.Filhos[1].Filhos[0].Rotulo);
        Assert.Equal(TimeSpan.FromMinutes(10), dia.Filhos[1].DuracaoTotal);
    }

    [Fact]
    public void Separa_os_dias_com_o_mais_recente_primeiro()
    {
        var ontem = new DateTimeOffset(2026, 11, 16, 12, 0, 0, TimeSpan.FromHours(-3));
        var hoje = new DateTimeOffset(2026, 11, 17, 12, 0, 0, TimeSpan.FromHours(-3));
        var grupos = AgrupadorDeEsforco.Agrupar(
            [Fechado("Word", "a.docx", ontem, ontem.AddMinutes(5)), Fechado("Chrome", "Gmail", hoje, hoje.AddMinutes(5))],
            hoje.AddHours(1));

        Assert.Equal("ter. 17/11", grupos[0].Rotulo);
        Assert.Equal("Chrome", grupos[0].Filhos[0].Rotulo);
        Assert.Equal("seg. 16/11", grupos[1].Rotulo);
        Assert.Equal("Word", grupos[1].Filhos[0].Rotulo);
    }

    private static IntervaloDeEsforco Fechado(string programa, string contexto, DateTimeOffset inicio, DateTimeOffset fim)
    {
        var intervalo = IntervaloDeEsforco.Abrir(programa, contexto, contexto, "", false, inicio);
        intervalo.Fechar(fim);
        return intervalo;
    }
}
