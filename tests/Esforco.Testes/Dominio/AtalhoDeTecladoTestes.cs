using Esforco.Dominio;

namespace Esforco.Testes.Dominio;

public class AtalhoDeTecladoTestes
{
    [Fact]
    public void Padrao_e_ctrl_alt_e()
    {
        var atalho = AtalhoDeTeclado.Padrao;

        Assert.Equal("Ctrl+Alt+E", atalho.ToString());
        Assert.Equal('E', atalho.Codigo);
    }

    [Theory]
    [InlineData("Ctrl+Alt+E", "Ctrl+Alt+E", 0x45)]
    [InlineData(" ctrl + shift + f9 ", "Ctrl+Shift+F9", 0x78)]
    [InlineData("Win+Alt+7", "Alt+Win+7", 0x37)]
    [InlineData("alt+f12", "Alt+F12", 0x7B)]
    public void Le_o_texto_guardado(string texto, string esperado, int codigo)
    {
        var atalho = AtalhoDeTeclado.Ler(texto);

        Assert.Equal(esperado, atalho!.ToString());
        Assert.Equal(codigo, atalho.Codigo);
    }

    [Theory]
    [InlineData("")]
    [InlineData("E")]
    [InlineData("Shift+E")]
    [InlineData("Ctrl+Alt")]
    [InlineData("Ctrl+Alt+F13")]
    [InlineData("Ctrl+Alt+E+R")]
    [InlineData("Ctrl+Alt+ç")]
    public void Recusa_combinacao_sem_modificador_ou_com_tecla_que_nao_serve(string texto)
    {
        Assert.Null(AtalhoDeTeclado.Ler(texto));
    }

    [Fact]
    public void Explica_por_que_recusou()
    {
        Assert.Equal("Use Ctrl, Alt ou Win junto com a tecla.", AtalhoDeTeclado.Impedir(false, false, false, "E"));
        Assert.Equal("Escolha uma letra, um número ou uma tecla de F1 a F12.", AtalhoDeTeclado.Impedir(true, false, false, "Enter"));
        Assert.Null(AtalhoDeTeclado.Impedir(true, true, false, "e"));
    }
}
