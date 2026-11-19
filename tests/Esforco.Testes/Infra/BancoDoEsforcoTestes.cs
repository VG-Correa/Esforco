using Esforco.Infra;

namespace Esforco.Testes.Infra;

public class BancoDoEsforcoTestes : IDisposable
{
    private readonly string _raiz = Path.Combine(Path.GetTempPath(), "esforco-banco", Guid.NewGuid().ToString("N"));

    public BancoDoEsforcoTestes()
    {
        Directory.CreateDirectory(_raiz);
        File.WriteAllText(Path.Combine(_raiz, BancoDoEsforco.MarcadorDaRaiz), "<Solution />");
    }

    [Fact]
    public void Banco_fica_em_db_na_raiz_do_repositorio()
    {
        var bin = Path.Combine(_raiz, "src", "Esforco.Ui", "bin");
        Directory.CreateDirectory(bin);

        var caminho = BancoDoEsforco.Resolver(bin);

        Assert.Equal(Path.Combine(_raiz, "db", "esforco.db"), caminho);
    }

    [Fact]
    public void Copia_o_banco_antigo_uma_vez_e_nao_substitui()
    {
        var bin = Path.Combine(_raiz, "src", "Esforco.Ui", "bin");
        var saida = Path.Combine(_raiz, "saida");
        Directory.CreateDirectory(bin);
        Directory.CreateDirectory(saida);
        File.WriteAllText(Path.Combine(bin, "esforco.db"), "do-bin");
        File.WriteAllText(Path.Combine(saida, "esforco.db"), "da-saida");

        var caminho = BancoDoEsforco.Resolver(bin);
        Assert.Equal("do-bin", File.ReadAllText(caminho));

        File.WriteAllText(Path.Combine(bin, "esforco.db"), "novo");
        BancoDoEsforco.Resolver(bin);
        Assert.Equal("do-bin", File.ReadAllText(caminho));
    }

    [Fact]
    public void Sem_repositorio_o_banco_fica_ao_lado_do_executavel()
    {
        var pasta = Path.Combine(Path.GetTempPath(), "esforco-solto", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(pasta);

        try
        {
            var caminho = BancoDoEsforco.Resolver(pasta);
            Assert.Equal(Path.Combine(pasta, "esforco.db"), caminho);
        }
        finally
        {
            Directory.Delete(pasta, true);
        }
    }

    [Fact]
    public void Raiz_informada_vence_a_pasta_do_executavel()
    {
        var outro = Path.Combine(Path.GetTempPath(), "esforco-outro", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outro);

        try
        {
            var caminho = BancoDoEsforco.Resolver(outro, raizInformada: _raiz);
            Assert.Equal(Path.Combine(_raiz, "db", "esforco.db"), caminho);
        }
        finally
        {
            Directory.Delete(outro, true);
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(_raiz))
            Directory.Delete(_raiz, true);
    }
}
