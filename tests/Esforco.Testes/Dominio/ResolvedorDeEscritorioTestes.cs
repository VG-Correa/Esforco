using Esforco.Dominio;

namespace Esforco.Testes.Dominio;

public class ResolvedorDeEscritorioTestes
{
    [Fact]
    public void Sem_regra_o_escritorio_fica_vazio()
    {
        Assert.Equal("", ResolvedorDeEscritorio.Resolver("Planilha - Excel", []));
        Assert.Equal("", ResolvedorDeEscritorio.Resolver("Planilha - Excel", null));
    }

    [Fact]
    public void Titulo_que_nao_casa_deixa_o_escritorio_vazio()
    {
        var regras = new[] { new RegraDeEscritorio("Centro", ["escritorio centro"]) };

        Assert.Equal("", ResolvedorDeEscritorio.Resolver("Caixa de entrada - Google Chrome", regras));
    }

    [Fact]
    public void Casa_o_titulo_sem_distinguir_maiusculas()
    {
        var regras = new[] { new RegraDeEscritorio("Centro", ["centro"]) };

        Assert.Equal("Centro", ResolvedorDeEscritorio.Resolver("arquivo CENTRO.xlsx - Excel", regras));
    }

    [Fact]
    public void A_primeira_regra_que_casa_vence()
    {
        var regras = new[]
        {
            new RegraDeEscritorio("Primeiro", ["planilha"]),
            new RegraDeEscritorio("Segundo", ["planilha"]),
        };

        Assert.Equal("Primeiro", ResolvedorDeEscritorio.Resolver("Planilha mensal - Excel", regras));
    }

    [Fact]
    public void Usa_a_regra_seguinte_quando_a_primeira_nao_casa()
    {
        var regras = new[]
        {
            new RegraDeEscritorio("Norte", ["norte"]),
            new RegraDeEscritorio("Sul", ["sul"]),
        };

        Assert.Equal("Sul", ResolvedorDeEscritorio.Resolver("Relatório Sul - Word", regras));
    }

    [Fact]
    public void Trecho_curto_ou_escritorio_em_branco_nao_casa()
    {
        Assert.Equal("", ResolvedorDeEscritorio.Resolver("a planilha", [new RegraDeEscritorio("X", ["a"])]));
        Assert.Equal("", ResolvedorDeEscritorio.Resolver("titulo abc", [new RegraDeEscritorio("  ", ["abc"])]));
        Assert.Equal("", ResolvedorDeEscritorio.Resolver("  ", [new RegraDeEscritorio("X", ["planilha"])]));
    }

    [Fact]
    public void Casa_uma_frase_no_meio_do_titulo()
    {
        var regras = new[] { new RegraDeEscritorio("SplitC", ["SplitC São Paulo"]) };

        Assert.Equal("SplitC", ResolvedorDeEscritorio.Resolver("Cliente SplitC São Paulo - Chrome", regras));
    }
}
