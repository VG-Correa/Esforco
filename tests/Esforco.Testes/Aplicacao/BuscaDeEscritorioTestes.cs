using Esforco.Aplicacao;

namespace Esforco.Testes.Aplicacao;

public class BuscaDeEscritorioTestes
{
    private static readonly string[] Todos = ["1905", "2200", "3300"];

    [Fact]
    public void Busca_acha_pelo_pedaco_do_nome_sem_ligar_para_acento_ou_maiuscula()
    {
        string[] nomes = ["Concha y Toro  ·  id 1905", "Galena Advocacia  ·  id 2200", "São João  ·  id 3300"];

        Assert.Equal([nomes[1]], BuscaDeEscritorio.Filtrar(nomes, " galena ", item => item));
        Assert.Equal([nomes[2]], BuscaDeEscritorio.Filtrar(nomes, "sao joao", item => item));
        Assert.Equal([nomes[0]], BuscaDeEscritorio.Filtrar(nomes, "1905", item => item));
        Assert.Equal(nomes, BuscaDeEscritorio.Filtrar(nomes, "  ", item => item));
        Assert.Empty(BuscaDeEscritorio.Filtrar(nomes, "nada", item => item));
    }

    [Fact]
    public void Escritorio_do_vinculo_salvo_abre_marcado()
    {
        Assert.Equal("2200", BuscaDeEscritorio.Marcar("2200", null, Todos, comBusca: false));
    }

    [Fact]
    public void Sem_escolha_e_sem_busca_nada_fica_marcado()
    {
        Assert.Null(BuscaDeEscritorio.Marcar(null, null, Todos, comBusca: false));
    }

    [Fact]
    public void Escrever_na_busca_nao_troca_o_escritorio_escolhido_enquanto_ele_aparece()
    {
        Assert.Equal("2200", BuscaDeEscritorio.Marcar("2200", "2200", ["1905", "2200"], comBusca: true));
    }

    [Fact]
    public void Busca_que_esconde_o_escolhido_marca_o_primeiro_achado()
    {
        Assert.Equal("1905", BuscaDeEscritorio.Marcar("2200", null, ["1905", "3300"], comBusca: true));
    }

    [Fact]
    public void Apagar_a_busca_devolve_o_escritorio_escolhido()
    {
        Assert.Equal("2200", BuscaDeEscritorio.Marcar("2200", "1905", Todos, comBusca: false));
    }

    [Fact]
    public void Primeiro_achado_continua_marcado_quando_a_busca_e_apagada_sem_escolha_anterior()
    {
        Assert.Equal("3300", BuscaDeEscritorio.Marcar(null, "3300", Todos, comBusca: false));
    }

    [Fact]
    public void Busca_sem_achados_nao_marca_nada()
    {
        Assert.Null(BuscaDeEscritorio.Marcar("2200", "2200", [], comBusca: true));
    }
}
