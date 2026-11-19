using Esforco.Infra;

namespace Esforco.Testes.Infra;

public class FonteDeRegrasDeEscritorioJsonTestes
{
    [Fact]
    public void Arquivo_ausente_grava_o_modelo_sem_regra_ativa()
    {
        var caminho = Caminho();
        var fonte = new FonteDeRegrasDeEscritorioJson(caminho);

        var regras = fonte.Carregar();

        Assert.Empty(regras);
        Assert.Contains("exemplo", File.ReadAllText(caminho), StringComparison.OrdinalIgnoreCase);
        Assert.Empty(fonte.Carregar());
    }

    [Fact]
    public void Nao_substitui_arquivo_existente()
    {
        var caminho = Caminho();
        File.WriteAllText(caminho, """{"regras":[{"escritorio":"A","contem":["xyz-unico"]}]}""");
        var antes = File.ReadAllBytes(caminho);
        var fonte = new FonteDeRegrasDeEscritorioJson(caminho);

        Assert.Equal("A", fonte.Carregar()[0].Escritorio);
        Assert.Equal(antes, File.ReadAllBytes(caminho));
    }

    [Fact]
    public void Exemplo_fora_de_regras_nao_entra_em_vigor()
    {
        var caminho = Caminho();
        File.WriteAllText(caminho, """
            {
              "exemplo": { "escritorio": "Nao usar", "contem": ["qualquer"] },
              "regras": []
            }
            """);

        Assert.Empty(new FonteDeRegrasDeEscritorioJson(caminho).Carregar());
    }

    [Fact]
    public void Le_regra_e_aceita_comentario()
    {
        var caminho = Caminho();
        File.WriteAllText(caminho, """
            {
              // regra de exemplo
              "regras": [ { "escritorio": "Centro", "contem": ["alpha-beta"] } ]
            }
            """);

        var regras = new FonteDeRegrasDeEscritorioJson(caminho).Carregar();

        Assert.Equal("Centro", regras[0].Escritorio);
        Assert.Equal("alpha-beta", regras[0].Contem[0]);
    }

    [Fact]
    public void Json_invalido_nao_apaga_regra_ja_carregada()
    {
        var caminho = Caminho();
        File.WriteAllText(caminho, """{"regras":[{"escritorio":"Centro","contem":["alpha"]}]}""");
        var fonte = new FonteDeRegrasDeEscritorioJson(caminho);
        Assert.Equal("Centro", fonte.Carregar()[0].Escritorio);

        File.WriteAllText(caminho, "{");
        File.SetLastWriteTimeUtc(caminho, DateTime.UtcNow.AddMinutes(5));

        Assert.Equal("Centro", fonte.Carregar()[0].Escritorio);
    }

    [Fact]
    public void Recarrega_quando_o_arquivo_muda()
    {
        var caminho = Caminho();
        File.WriteAllText(caminho, """{"regras":[{"escritorio":"A","contem":["alpha-1"]}]}""");
        var fonte = new FonteDeRegrasDeEscritorioJson(caminho);
        Assert.Equal("A", fonte.Carregar()[0].Escritorio);

        File.WriteAllText(caminho, """{"regras":[{"escritorio":"B","contem":["beta-2"]}]}""");
        File.SetLastWriteTimeUtc(caminho, DateTime.UtcNow.AddMinutes(5));

        Assert.Equal("B", fonte.Carregar()[0].Escritorio);
    }

    [Fact]
    public void Arquivo_publicado_nao_traz_regra_ativa()
    {
        var caminho = Path.Combine(Raiz(), "src", "Esforco.Ui", "regras-escritorio.json");
        var antes = File.ReadAllBytes(caminho);

        var regras = new FonteDeRegrasDeEscritorioJson(caminho).Carregar();

        Assert.Empty(regras);
        Assert.Equal(antes, File.ReadAllBytes(caminho));
    }

    private static string Caminho()
    {
        var pasta = Path.Combine(Path.GetTempPath(), "esforco-testes", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(pasta);
        return Path.Combine(pasta, "regras-escritorio.json");
    }

    private static string Raiz()
    {
        var pasta = new DirectoryInfo(AppContext.BaseDirectory);
        while (pasta is not null && !File.Exists(Path.Combine(pasta.FullName, "Esforco.slnx")))
            pasta = pasta.Parent;

        Assert.NotNull(pasta);
        return pasta.FullName;
    }
}
