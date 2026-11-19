using Esforco.Dominio;

namespace Esforco.Testes.Dominio;

public class IntervaloDeEsforcoTestes
{
    private static readonly DateTimeOffset Inicio = new(2026, 11, 17, 10, 0, 0, TimeSpan.FromHours(-3));

    [Fact]
    public void Duracao_enquanto_aberto_usa_o_instante_pedido()
    {
        var intervalo = IntervaloDeEsforco.Abrir("Excel", "a.xlsx - Excel", "a.xlsx", "", false, Inicio);

        Assert.Equal(TimeSpan.FromMinutes(3), intervalo.DuracaoAte(Inicio.AddMinutes(3)));
        Assert.Null(intervalo.Fim);
    }

    [Fact]
    public void Fechar_grava_o_fim_e_a_duracao()
    {
        var intervalo = IntervaloDeEsforco.Abrir("Excel", "a.xlsx", "a.xlsx", "", false, Inicio);

        intervalo.Fechar(Inicio.AddMinutes(12));

        Assert.Equal(Inicio.AddMinutes(12), intervalo.Fim);
        Assert.Equal(TimeSpan.FromMinutes(12), intervalo.DuracaoAte(Inicio.AddHours(2)));
    }

    [Fact]
    public void Fechar_de_novo_mantem_o_primeiro_fim()
    {
        var intervalo = IntervaloDeEsforco.Abrir("Excel", "a.xlsx", "a.xlsx", "", false, Inicio);

        intervalo.Fechar(Inicio.AddMinutes(1));
        intervalo.Fechar(Inicio.AddMinutes(9));

        Assert.Equal(Inicio.AddMinutes(1), intervalo.Fim);
    }

    [Fact]
    public void Fechar_antes_do_inicio_nao_gera_duracao_negativa()
    {
        var intervalo = IntervaloDeEsforco.Abrir("Excel", "a.xlsx", "a.xlsx", "", false, Inicio);

        intervalo.Fechar(Inicio.AddMinutes(-5));

        Assert.Equal(Inicio, intervalo.Fim);
        Assert.Equal(TimeSpan.Zero, intervalo.DuracaoAte(Inicio));
    }

    [Fact]
    public void Continua_enquanto_programa_titulo_e_minimizada_forem_os_mesmos()
    {
        var intervalo = IntervaloDeEsforco.Abrir("Excel", "a.xlsx", "a.xlsx", "Centro", false, Inicio);

        Assert.True(intervalo.Continua(" Excel ", "a.xlsx", false));
        Assert.False(intervalo.Continua("Word", "a.xlsx", false));
        Assert.False(intervalo.Continua("Excel", "b.xlsx", false));
        Assert.False(intervalo.Continua("Excel", "a.xlsx", true));
    }

    [Fact]
    public void Continua_fica_falso_depois_de_fechar()
    {
        var intervalo = IntervaloDeEsforco.Abrir("Excel", "a.xlsx", "a.xlsx", "", false, Inicio);
        intervalo.Fechar(Inicio.AddMinutes(1));

        Assert.False(intervalo.Continua("Excel", "a.xlsx", false));
    }

    [Fact]
    public void Abrir_normaliza_programa_vazio_e_escritorio_em_branco()
    {
        var intervalo = IntervaloDeEsforco.Abrir("  ", " titulo ", " ctx ", "  ", false, Inicio);

        Assert.Equal("Programa desconhecido", intervalo.Programa);
        Assert.Equal("titulo", intervalo.Titulo);
        Assert.Equal("ctx", intervalo.Contexto);
        Assert.Equal("", intervalo.Escritorio);
    }
}
