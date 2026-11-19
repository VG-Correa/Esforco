using Esforco.Dominio;

namespace Esforco.Testes.Dominio;

public class TabelaDeTransferenciaTestes
{
    [Fact]
    public void Copia_codigo_celula_vazia_percentual_e_nome_sem_titulo()
    {
        var texto = TabelaDeTransferencia.Montar(
        [
            ("1905", "62%", "VCT"),
            ("2200", "38%", "NTM"),
        ]);

        Assert.Equal("1905\t\t62%\tVCT\r\n2200\t\t38%\tNTM", texto);
        Assert.DoesNotContain("Código", texto);
        Assert.DoesNotContain("Percentual", texto);
    }

    [Fact]
    public void Nome_sem_id_fica_de_fora()
    {
        var texto = TabelaDeTransferencia.Montar(
        [
            ("", "0%", "Outro nome"),
            ("3300", "100%", "Escritório 3300"),
        ]);

        Assert.Equal("3300\t\t100%\tEscritório 3300", texto);
    }
}
