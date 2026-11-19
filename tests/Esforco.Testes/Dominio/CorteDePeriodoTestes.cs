using Esforco.Dominio;

namespace Esforco.Testes.Dominio;

public class CorteDePeriodoTestes
{
    private static readonly TimeSpan Zona = TimeSpan.FromHours(-3);

    private static DateTimeOffset Em(int dia, int hora, int minuto = 0) =>
        new(2026, 11, dia, hora, minuto, 0, Zona);

    [Fact]
    public void Apaga_o_intervalo_que_cabe_inteiro_no_periodo()
    {
        var intervalo = Fechado(1, Em(17, 10), Em(17, 11));
        var plano = CorteDePeriodo.Planejar([intervalo], Em(17, 0), Em(18, 0), Em(17, 12));

        Assert.Equal([1L], plano.Excluir);
        Assert.Empty(plano.Atualizar);
        Assert.Empty(plano.Criar);
    }

    [Fact]
    public void Encurta_o_intervalo_que_entra_no_periodo()
    {
        var intervalo = Fechado(2, Em(16, 22), Em(17, 2));
        var plano = CorteDePeriodo.Planejar([intervalo], Em(17, 0), Em(18, 0), Em(17, 12));

        Assert.Empty(plano.Excluir);
        Assert.Equal(Em(16, 22), intervalo.Inicio);
        Assert.Equal(Em(17, 0), intervalo.Fim);
        Assert.Empty(plano.Criar);
    }

    [Fact]
    public void Adianta_o_inicio_quando_o_intervalo_sai_do_periodo()
    {
        var intervalo = Fechado(3, Em(17, 22), Em(18, 3));
        var plano = CorteDePeriodo.Planejar([intervalo], Em(17, 0), Em(18, 0), Em(18, 4));

        Assert.Equal(Em(18, 0), intervalo.Inicio);
        Assert.Equal(Em(18, 3), intervalo.Fim);
        Assert.Empty(plano.Criar);
    }

    [Fact]
    public void Divide_o_intervalo_que_atravessa_o_periodo()
    {
        var intervalo = Fechado(4, Em(16, 20), Em(18, 8));
        var plano = CorteDePeriodo.Planejar([intervalo], Em(17, 0), Em(18, 0), Em(18, 9));

        Assert.Equal(Em(16, 20), intervalo.Inicio);
        Assert.Equal(Em(17, 0), intervalo.Fim);
        var continuacao = Assert.Single(plano.Criar);
        Assert.Equal(Em(18, 0), continuacao.Inicio);
        Assert.Equal(Em(18, 8), continuacao.Fim);
        Assert.Equal("Word", continuacao.Programa);
    }

    [Fact]
    public void Intervalo_aberto_que_comecou_antes_perde_so_o_trecho_de_hoje()
    {
        var intervalo = IntervaloDeEsforco.Abrir("Chrome", "Gmail", "Gmail", "", false, Em(16, 23));
        intervalo.DefinirId(5);
        var plano = CorteDePeriodo.Planejar([intervalo], Em(17, 0), Em(18, 0), Em(17, 9));

        Assert.Equal(Em(17, 0), intervalo.Fim);
        Assert.Empty(plano.Criar);
        Assert.Empty(plano.Excluir);
    }

    [Fact]
    public void Semana_comeca_na_segunda()
    {
        var terca = new DateTimeOffset(2026, 11, 17, 15, 0, 0, Zona);
        var semana = PeriodoLocal.Semana(terca);

        Assert.Equal(new DateTimeOffset(2026, 11, 16, 0, 0, 0, Zona), semana.Inicio);
        Assert.Equal(new DateTimeOffset(2026, 11, 23, 0, 0, 0, Zona), semana.Fim);
    }

    private static IntervaloDeEsforco Fechado(long id, DateTimeOffset inicio, DateTimeOffset fim)
    {
        var intervalo = IntervaloDeEsforco.Abrir("Word", "relatorio.docx", "relatorio.docx", "", false, inicio);
        intervalo.DefinirId(id);
        intervalo.Fechar(fim);
        return intervalo;
    }
}
