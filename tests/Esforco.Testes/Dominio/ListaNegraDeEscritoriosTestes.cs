using Esforco.Dominio;

namespace Esforco.Testes.Dominio;

public class ListaNegraDeEscritoriosTestes
{
    [Fact]
    public void Id_ou_nome_tiram_o_escritorio_do_relatorio()
    {
        var straumann = Linha("1759", "Straumann", "Straumann", ["Straumann LATAM"]);
        var vct = Linha("1905", "VCT", "VCT", []);
        var outro = Linha("", "", "Outro nome", []);

        var semId = ListaNegraDeEscritorios.Visiveis(
            [straumann, vct, outro],
            [new OcultacaoDeEscritorio(true, "1759")]);
        Assert.Equal(["1905", ""], semId.Select(item => item.Id));

        var semNome = ListaNegraDeEscritorios.Visiveis(
            [straumann, vct, outro],
            [new OcultacaoDeEscritorio(false, "straumann latam")]);
        Assert.Equal(["1905", ""], semNome.Select(item => item.Id));

        var semSolto = ListaNegraDeEscritorios.Visiveis(
            [straumann, vct, outro],
            [new OcultacaoDeEscritorio(false, "Outro nome")]);
        Assert.Equal(["1759", "1905"], semSolto.Select(item => item.Id));
    }

    [Fact]
    public void Nome_vazio_nao_esconde_a_lista()
    {
        var aviso = ListaNegraDeEscritorios.Impedir(false, "   ", []);
        Assert.Equal("Informe o nome.", aviso);

        var linhas = ListaNegraDeEscritorios.Visiveis(
            [Linha("1759", "Straumann", "Straumann", [])],
            [new OcultacaoDeEscritorio(false, " ")]);
        Assert.Single(linhas);
    }

    private static LinhaDeEscritorio Linha(string id, string nome, string rotulo, string[] variacoes) =>
        new(
            "id:" + id,
            id,
            rotulo,
            nome,
            variacoes,
            TimeSpan.Zero,
            TimeSpan.Zero,
            0,
            TimeSpan.Zero,
            [],
            [],
            id.Length > 0);
}
