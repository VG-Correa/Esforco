using Esforco.Dominio;

namespace Esforco.Testes.Dominio;

public class ExtratorDeContextoTestes
{
    [Theory]
    [InlineData("relatorio.xlsx - Excel", "relatorio.xlsx")]
    [InlineData(@"C:\Users\victor\notas.txt - Notepad", "notas.txt")]
    [InlineData("*proposta.docx - Word", "proposta.docx")]
    [InlineData("Esforco.cs - Esforco - Cursor", "Esforco.cs")]
    [InlineData("Caixa de entrada - Google Chrome", "Caixa de entrada")]
    [InlineData("Pré-venda - Google Chrome", "Pré-venda")]
    [InlineData("pasta/arquivo.pdf - Leitor", "arquivo.pdf")]
    [InlineData("somente um titulo", "somente um titulo")]
    [InlineData("   ", "")]
    [InlineData(null, "")]
    public void Extrai_aba_pagina_ou_arquivo(string? titulo, string esperado)
    {
        Assert.Equal(esperado, ExtratorDeContexto.Extrair(titulo));
    }
}
