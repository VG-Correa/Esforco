using Esforco.Dominio;

namespace Esforco.Testes.Dominio;

public class ColunaDoCalendarioTestes
{
    private const double Largura = 400;

    [Fact]
    public void Sem_registro_manual_as_faixas_que_coincidem_seguem_em_cascata_como_antes()
    {
        var lugares = ColunaDoCalendario.Distribuir(
            [
                Automatica(10, 0, 11, 0),
                Automatica(10, 10, 10, 40),
                Automatica(10, 20, 10, 30),
                Automatica(13, 0, 13, 30),
            ],
            Largura);

        Assert.Equal(
            [
                new LugarDaFaixa(0, 400, 400),
                new LugarDaFaixa(16, 384, 400),
                new LugarDaFaixa(32, 368, 400),
                new LugarDaFaixa(0, 400, 400),
            ],
            lugares);
    }

    [Fact]
    public void Registro_manual_fica_ao_lado_da_faixa_automatica_do_mesmo_horario()
    {
        var lugares = ColunaDoCalendario.Distribuir(
            [
                Manual(10, 20, 11, 30),
                Automatica(10, 0, 10, 40),
            ],
            Largura);

        Assert.Equal(new LugarDaFaixa(200, 200, 200), lugares[0]);
        Assert.Equal(new LugarDaFaixa(0, 200, 200), lugares[1]);
    }

    [Fact]
    public void Registro_manual_sozinho_no_horario_ocupa_a_coluna_inteira()
    {
        var lugares = ColunaDoCalendario.Distribuir(
            [
                Manual(14, 0, 15, 0),
                Automatica(10, 0, 10, 40),
                Manual(10, 40, 11, 0),
            ],
            Largura);

        Assert.All(lugares, lugar => Assert.Equal(new LugarDaFaixa(0, 400, 400), lugar));
    }

    [Fact]
    public void Dois_registros_manuais_no_horario_de_uma_faixa_automatica_fazem_cascata_na_metade_deles()
    {
        var lugares = ColunaDoCalendario.Distribuir(
            [
                Manual(10, 20, 11, 30),
                Automatica(10, 0, 10, 40),
                Manual(10, 30, 11, 0),
                Automatica(10, 5, 10, 15),
            ],
            Largura);

        Assert.Equal(
            [
                new LugarDaFaixa(200, 200, 200),
                new LugarDaFaixa(0, 200, 200),
                new LugarDaFaixa(216, 184, 200),
                new LugarDaFaixa(16, 384, 400),
            ],
            lugares);
    }

    [Fact]
    public void Faixa_automatica_que_nao_toca_em_registro_manual_recua_sobre_a_que_esta_atras()
    {
        var lugares = ColunaDoCalendario.Distribuir(
            [
                Automatica(10, 0, 12, 0),
                Manual(11, 0, 11, 30),
                Automatica(10, 5, 10, 15),
            ],
            Largura);

        Assert.Equal(new LugarDaFaixa(0, 200, 200), lugares[0]);
        Assert.Equal(new LugarDaFaixa(200, 200, 200), lugares[1]);
        Assert.Equal(new LugarDaFaixa(16, 384, 400), lugares[2]);
    }

    [Fact]
    public void Em_coluna_estreita_o_recuo_nao_deixa_a_faixa_menor_que_o_minimo()
    {
        var lugares = ColunaDoCalendario.Distribuir(
            [
                Manual(10, 0, 12, 0),
                Automatica(10, 0, 11, 0),
                Manual(10, 10, 10, 50),
                Manual(10, 20, 10, 40),
            ],
            190);

        Assert.Equal(new LugarDaFaixa(95, 95, 95), lugares[0]);
        Assert.Equal(new LugarDaFaixa(0, 95, 95), lugares[1]);
        Assert.Equal(new LugarDaFaixa(102, 88, 95), lugares[2]);
        Assert.Equal(new LugarDaFaixa(102, 88, 95), lugares[3]);
    }

    [Fact]
    public void Faixas_que_so_se_encostam_nao_dividem_a_coluna()
    {
        var lugares = ColunaDoCalendario.Distribuir(
            [
                Automatica(10, 0, 10, 30),
                Manual(10, 30, 11, 0),
            ],
            Largura);

        Assert.All(lugares, lugar => Assert.Equal(new LugarDaFaixa(0, 400, 400), lugar));
    }

    private static FaixaNaColuna Automatica(int hora, int minuto, int ateHora, int ateMinuto) =>
        new(hora * 3600 + minuto * 60, ateHora * 3600 + ateMinuto * 60, false);

    private static FaixaNaColuna Manual(int hora, int minuto, int ateHora, int ateMinuto) =>
        new(hora * 3600 + minuto * 60, ateHora * 3600 + ateMinuto * 60, true);
}
