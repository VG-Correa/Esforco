using Esforco.Dominio;
using static Esforco.Testes.Apoio.Vinculos;

namespace Esforco.Testes.Dominio;

public class CategoriaTestes
{
    private static readonly Categoria Contratos = new(1, "Contratos");
    private static readonly Categoria Suporte = new(2, "Suporte");

    [Fact]
    public void Categoria_sem_uso_e_excluida_sem_pergunta()
    {
        var plano = CadastroDeCategorias.PlanejarExclusao(1, [Contratos, Suporte], [], null);

        Assert.True(plano.Pode);
        Assert.Equal(0, plano.EmUso);
        Assert.Null(plano.Aviso);
    }

    [Fact]
    public void Categoria_em_uso_nao_e_excluida_sem_escolha()
    {
        var vinculos = new[]
        {
            Novo(1, "100", AlcanceDoVinculo.Programa, "Word", categoria: 1),
            Novo(2, "100", AlcanceDoVinculo.Programa, "Excel", categoria: 1),
            Novo(3, "100", AlcanceDoVinculo.Programa, "Bloco de Notas", categoria: 2),
        };

        var plano = CadastroDeCategorias.PlanejarExclusao(1, [Contratos, Suporte], vinculos, null);

        Assert.False(plano.Pode);
        Assert.Equal(2, plano.EmUso);
        Assert.Contains("2 vínculos", plano.Aviso);
    }

    [Fact]
    public void Categoria_em_uso_pode_ser_trocada_por_outra_ou_retirada()
    {
        var vinculos = new[] { Novo(1, "100", AlcanceDoVinculo.Programa, "Word", categoria: 1) };

        var troca = CadastroDeCategorias.PlanejarExclusao(1, [Contratos, Suporte], vinculos, EscolhaAoExcluirCategoria.Trocar(2));
        var retirada = CadastroDeCategorias.PlanejarExclusao(1, [Contratos, Suporte], vinculos, EscolhaAoExcluirCategoria.Retirar);

        Assert.True(troca.Pode);
        Assert.Equal(2, troca.TrocarPor);
        Assert.True(retirada.Pode);
        Assert.Null(retirada.TrocarPor);
    }

    [Fact]
    public void Troca_pela_propria_categoria_ou_por_uma_que_nao_existe_e_recusada()
    {
        var vinculos = new[] { Novo(1, "100", AlcanceDoVinculo.Programa, "Word", categoria: 1) };

        var propria = CadastroDeCategorias.PlanejarExclusao(1, [Contratos, Suporte], vinculos, EscolhaAoExcluirCategoria.Trocar(1));
        var sumida = CadastroDeCategorias.PlanejarExclusao(1, [Contratos, Suporte], vinculos, EscolhaAoExcluirCategoria.Trocar(9));

        Assert.False(propria.Pode);
        Assert.False(sumida.Pode);
    }

    [Fact]
    public void Nome_vazio_ou_repetido_e_recusado()
    {
        Assert.NotNull(CadastroDeCategorias.ImpedirNome("  ", [Contratos]));
        Assert.Equal("Essa categoria já existe.", CadastroDeCategorias.ImpedirNome(" contratos ", [Contratos]));
        Assert.Null(CadastroDeCategorias.ImpedirNome("Contratos", [Contratos], ignorar: 1));
        Assert.Null(CadastroDeCategorias.ImpedirNome("Reuniões externas", [Contratos]));
    }
}
