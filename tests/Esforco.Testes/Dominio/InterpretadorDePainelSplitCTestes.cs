using Esforco.Dominio;

namespace Esforco.Testes.Dominio;

public class InterpretadorDePainelSplitCTestes
{
    [Theory]
    [InlineData(
        "https://app.splitc.com.br/planos?officeId=2200",
        "Painel SplitC > Escritório 2200 > Lista de planos")]
    [InlineData(
        "https://app.splitc.com.br/planos/17927/tab/862247?officeId=2200&periodId=49797&tabSelector=source&pagina=0&coluna=bq4&linha=19&rowId=19",
        "Painel SplitC > Escritório 2200 > Plano 17927 > Período 49797 > Fontes > Aba 862247")]
    [InlineData(
        "https://app.splitc.com.br/planos/17927/tab/596648?officeId=2200&periodId=49797&tabSelector=plans&pagina=0&coluna=bq4&linha=19&rowId=19",
        "Painel SplitC > Escritório 2200 > Plano 17927 > Período 49797 > Cálculos > Aba 596648")]
    [InlineData(
        "https://app.splitc.com.br/planos/17927?officeId=2200&periodId=49797&tabSelector=validation",
        "Painel SplitC > Escritório 2200 > Plano 17927 > Período 49797 > Validação")]
    [InlineData(
        "https://app.splitc.com.br/planos/17927?officeId=2200&periodId=49797&tabSelector=statement",
        "Painel SplitC > Escritório 2200 > Plano 17927 > Período 49797 > Dashboards")]
    [InlineData(
        "https://app.splitc.com.br/planos/17927?officeId=2200&periodId=49797&tabSelector=statement&panelId=42734",
        "Painel SplitC > Escritório 2200 > Plano 17927 > Período 49797 > Dashboards > Dashboard 42734")]
    [InlineData(
        "https://app.splitc.com.br/planos/17927/tab/659856?officeId=2200&periodId=49797&tabSelector=payroll&panelId=42734&coluna=1&linha=1",
        "Painel SplitC > Escritório 2200 > Plano 17927 > Período 49797 > Usuários e pagamentos > Aba 659856")]
    [InlineData(
        "https://app.splitc.com.br/planos/17927/tab/659856?officeId=2200&periodId=49797&tabSelector=graph&panelId=42734&coluna=1&linha=1",
        "Painel SplitC > Escritório 2200 > Plano 17927 > Período 49797 > Painel de controle > Aba 659856")]
    public void Organiza_o_endereco_na_hierarquia(string endereco, string caminho)
    {
        var local = InterpretadorDePainelSplitC.Interpretar(endereco);

        Assert.NotNull(local);
        Assert.Equal(caminho, string.Join(" > ", CaminhoDeEsforco.DoPainel(local)));
    }

    [Fact]
    public void Outro_endereco_nao_e_o_painel()
    {
        Assert.Null(InterpretadorDePainelSplitC.Interpretar("https://mail.google.com/mail"));
        Assert.Null(InterpretadorDePainelSplitC.Interpretar(""));
        Assert.Null(InterpretadorDePainelSplitC.Interpretar(null));
    }

    [Fact]
    public void Trocar_a_celula_nao_muda_o_local()
    {
        var primeira = InterpretadorDePainelSplitC.Interpretar(
            "https://app.splitc.com.br/planos/17927/tab/862247?officeId=2200&periodId=49797&tabSelector=source&coluna=bq4&linha=19");
        var segunda = InterpretadorDePainelSplitC.Interpretar(
            "https://app.splitc.com.br/planos/17927/tab/862247?officeId=2200&periodId=49797&tabSelector=source&coluna=c1&linha=3&pagina=1&rowId=3");

        Assert.Equal(primeira, segunda);
    }
}
