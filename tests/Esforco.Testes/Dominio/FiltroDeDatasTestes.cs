using Esforco.Dominio;

namespace Esforco.Testes.Dominio;

public class FiltroDeDatasTestes
{
    private static DateTime Dia(int mes, int dia) => new(2026, mes, dia);

    [Fact]
    public void Semanal_vai_de_segunda_a_sexta()
    {
        var (inicio, fim) = FiltroDeDatas.Do(TipoDeFiltroDeDatas.Semanal, Dia(11, 17), false);

        Assert.Equal(Dia(11, 16), inicio);
        Assert.Equal(Dia(11, 20), fim);
    }

    [Fact]
    public void Semanal_com_fim_de_semana_vai_de_domingo_a_sabado()
    {
        var (inicio, fim) = FiltroDeDatas.Do(TipoDeFiltroDeDatas.Semanal, Dia(11, 17), true);

        Assert.Equal(Dia(11, 15), inicio);
        Assert.Equal(Dia(11, 21), fim);
    }

    [Fact]
    public void Domingo_entra_na_semana_que_comeca_nele()
    {
        var util = FiltroDeDatas.Do(TipoDeFiltroDeDatas.Semanal, Dia(11, 15), false);
        var completa = FiltroDeDatas.Do(TipoDeFiltroDeDatas.Semanal, Dia(11, 15), true);

        Assert.Equal((Dia(11, 9), Dia(11, 13)), util);
        Assert.Equal((Dia(11, 15), Dia(11, 21)), completa);
    }

    [Fact]
    public void Mensal_trimestral_semestral_e_anual_cobrem_o_bloco()
    {
        Assert.Equal((Dia(11, 1), Dia(11, 30)), FiltroDeDatas.Do(TipoDeFiltroDeDatas.Mensal, Dia(11, 17), false));
        Assert.Equal((Dia(10, 1), Dia(12, 31)), FiltroDeDatas.Do(TipoDeFiltroDeDatas.Trimestral, Dia(11, 17), false));
        Assert.Equal((Dia(7, 1), Dia(12, 31)), FiltroDeDatas.Do(TipoDeFiltroDeDatas.Semestral, Dia(11, 17), false));
        Assert.Equal((Dia(1, 1), Dia(12, 31)), FiltroDeDatas.Do(TipoDeFiltroDeDatas.Anual, Dia(11, 17), false));
    }

    [Fact]
    public void Setas_avancam_um_periodo_inteiro()
    {
        Assert.Equal(
            (Dia(12, 1), Dia(12, 31)),
            FiltroDeDatas.Deslocar(TipoDeFiltroDeDatas.Mensal, Dia(11, 1), Dia(11, 30), 1, false));
        Assert.Equal(
            (Dia(11, 23), Dia(11, 27)),
            FiltroDeDatas.Deslocar(TipoDeFiltroDeDatas.Semanal, Dia(11, 16), Dia(11, 20), 1, false));
        Assert.Equal(
            (new DateTime(2027, 1, 1), new DateTime(2027, 3, 31)),
            FiltroDeDatas.Deslocar(TipoDeFiltroDeDatas.Trimestral, Dia(10, 1), Dia(12, 31), 1, false));
    }

    [Fact]
    public void Ao_andar_o_periodo_a_referencia_permanece_no_mesmo_dia()
    {
        Assert.Equal(
            Dia(11, 25),
            FiltroDeDatas.ReferenciaAoDeslocar(Dia(11, 18), Dia(11, 16), Dia(11, 23), Dia(11, 27)));
        Assert.Equal(
            Dia(12, 18),
            FiltroDeDatas.ReferenciaAoDeslocar(Dia(11, 18), Dia(11, 1), Dia(12, 1), Dia(12, 31)));
        Assert.Equal(
            Dia(2, 28),
            FiltroDeDatas.ReferenciaAoDeslocar(Dia(1, 31), Dia(1, 1), Dia(2, 1), Dia(2, 28)));
    }
}
