using Esforco.Dominio;
using static Esforco.Testes.Apoio.Vinculos;

namespace Esforco.Testes.Dominio;

public class FormularioDeVinculoTestes
{
    private static readonly DateTimeOffset Agora = Em(17, 15, 0);

    [Fact]
    public void Pelo_atalho_sem_vinculo_abre_em_branco_a_partir_de_agora()
    {
        var alvo = new AlvoDeVinculo("Google Chrome", "Sala - Google Chrome", "app.gather.town/app/sala");

        var formulario = FormularioDeVinculo.Montar(alvo, [], Agora);

        Assert.Null(formulario.EmEdicao);
        Assert.False(formulario.Copia);
        Assert.Equal("", formulario.Escritorio);
        Assert.Null(formulario.CategoriaId);
        Assert.Equal(AlcanceDoVinculo.Aba, formulario.Alcance);
        Assert.False(formulario.OfereceRegistro);
        Assert.False(formulario.SoRegistro);
        Assert.Equal(ValidadeDoVinculo.Sempre, formulario.Validade);
        Assert.Equal(Agora, formulario.Inicio);
        Assert.Equal(TempoDeVinculo.Padrao, formulario.Tempo);
    }

    [Fact]
    public void Faixa_sem_vinculo_abre_marcando_so_o_registro_e_com_o_alcance_sugerido()
    {
        var alvo = new AlvoDeVinculo("Word", "Contrato - Word", "", Em(17, 10, 0), Em(17, 10, 40));

        var formulario = FormularioDeVinculo.Montar(alvo, [], Agora, AlcanceDoVinculo.Programa);

        Assert.Null(formulario.EmEdicao);
        Assert.True(formulario.OfereceRegistro);
        Assert.True(formulario.SoRegistro);
        Assert.Equal(AlcanceDoVinculo.Programa, formulario.Alcance);
        Assert.Equal(Em(17, 10, 0), formulario.Inicio);
    }

    [Fact]
    public void Faixa_com_vinculo_de_um_minuto_abre_com_tudo_o_que_foi_configurado()
    {
        var vinculo = Novo(
            7,
            "2200",
            AlcanceDoVinculo.Janela,
            "Word",
            "Contrato - Word",
            validade: ValidadeDoVinculo.Prazo,
            inicio: Em(17, 10, 3),
            duracao: TimeSpan.FromMinutes(1),
            categoria: 4);
        var alvo = new AlvoDeVinculo("Word", "Contrato - Word", "", Em(17, 10, 3), Em(17, 10, 4), VinculoId: 7);

        var formulario = FormularioDeVinculo.Montar(alvo, [vinculo], Agora, AlcanceDoVinculo.Programa);

        Assert.Equal(7, formulario.EmEdicao);
        Assert.False(formulario.Copia);
        Assert.Equal("2200", formulario.Escritorio);
        Assert.Equal(4, formulario.CategoriaId);
        Assert.Equal(AlcanceDoVinculo.Janela, formulario.Alcance);
        Assert.True(formulario.OfereceRegistro);
        Assert.False(formulario.SoRegistro);
        Assert.Equal(ValidadeDoVinculo.Prazo, formulario.Validade);
        Assert.Equal(Em(17, 10, 3), formulario.Inicio);
        Assert.Equal(new TempoDeVinculo(OpcaoDeTempo.Outro, 1, false), formulario.Tempo);
    }

    [Fact]
    public void Vinculo_por_inatividade_abre_com_a_validade_e_o_tempo_dele()
    {
        var vinculo = Novo(
            3,
            "2200",
            AlcanceDoVinculo.Programa,
            "Excel",
            validade: ValidadeDoVinculo.Inatividade,
            inicio: Em(17, 9, 0),
            duracao: TimeSpan.FromHours(2));
        var alvo = new AlvoDeVinculo("Excel", "custos.xlsx - Excel", VinculoId: 3);

        var formulario = FormularioDeVinculo.Montar(alvo, [vinculo], Agora);

        Assert.Equal(ValidadeDoVinculo.Inatividade, formulario.Validade);
        Assert.Equal(new TempoDeVinculo(OpcaoDeTempo.Outro, 2, true), formulario.Tempo);
        Assert.Equal(Em(17, 9, 0), formulario.Inicio);
    }

    [Fact]
    public void Registro_marcado_sozinho_abre_com_o_periodo_do_vinculo_e_nao_com_o_da_faixa()
    {
        var vinculo = Novo(
            5,
            "2200",
            AlcanceDoVinculo.Janela,
            "Word",
            "Contrato - Word",
            validade: ValidadeDoVinculo.Prazo,
            inicio: Em(17, 10, 0),
            duracao: TimeSpan.FromMinutes(40),
            soRegistro: true);
        var faixa = new AlvoDeVinculo("Word", "Contrato - Word", "", Em(17, 9, 58), Em(17, 10, 44), VinculoId: 5);

        var formulario = FormularioDeVinculo.Montar(faixa, [vinculo], Agora);

        Assert.Equal(5, formulario.EmEdicao);
        Assert.True(formulario.SoRegistro);
        Assert.Equal(Em(17, 10, 0), formulario.Alvo.Inicio);
        Assert.Equal(Em(17, 10, 40), formulario.Alvo.Fim);
        Assert.Equal(ValidadeDoVinculo.Sempre, formulario.Validade);
    }

    [Fact]
    public void Vinculo_do_programa_inteiro_guarda_o_titulo_da_janela_para_o_alcance_poder_mudar()
    {
        var vinculo = Novo(2, "2200", AlcanceDoVinculo.Programa, "Google Chrome");
        var alvo = new AlvoDeVinculo("Google Chrome", "Gmail - Google Chrome", "mail.google.com/mail", VinculoId: 2);

        var formulario = FormularioDeVinculo.Montar(alvo, [vinculo], Agora);

        Assert.Equal(AlcanceDoVinculo.Programa, formulario.Alcance);
        Assert.True(formulario.PodeJanela);
        Assert.True(formulario.PodeAba);
        Assert.Equal("Gmail - Google Chrome", formulario.Alvo.Titulo);
        Assert.Equal("mail.google.com/mail", formulario.Alvo.Endereco);
    }

    [Fact]
    public void Vinculo_de_aba_mantem_o_titulo_e_o_endereco_que_ele_guardou()
    {
        var vinculo = Novo(2, "2200", AlcanceDoVinculo.Aba, "Google Chrome", "Sala - Google Chrome", "app.gather.town/app/sala");
        var alvo = new AlvoDeVinculo("Google Chrome", "Outro nome - Google Chrome", "app.gather.town/app/sala", VinculoId: 2);

        var formulario = FormularioDeVinculo.Montar(alvo, [vinculo], Agora);

        Assert.Equal(AlcanceDoVinculo.Aba, formulario.Alcance);
        Assert.Equal("Sala - Google Chrome", formulario.Alvo.Titulo);
        Assert.Equal("app.gather.town/app/sala", formulario.Alvo.Endereco);
    }

    [Fact]
    public void Vinculo_que_ja_terminou_preenche_os_campos_mas_salvar_cria_outro_a_partir_de_agora()
    {
        var vinculo = Novo(
            9,
            "2200",
            AlcanceDoVinculo.Janela,
            "Word",
            "Contrato - Word",
            validade: ValidadeDoVinculo.Prazo,
            inicio: Em(17, 10, 0),
            duracao: TimeSpan.FromMinutes(1),
            categoria: 4);
        var alvo = new AlvoDeVinculo("Word", "Contrato - Word", CopiarDe: 9);

        var formulario = FormularioDeVinculo.Montar(alvo, [vinculo], Agora);

        Assert.Null(formulario.EmEdicao);
        Assert.True(formulario.Copia);
        Assert.Equal("2200", formulario.Escritorio);
        Assert.Equal(4, formulario.CategoriaId);
        Assert.Equal(AlcanceDoVinculo.Janela, formulario.Alcance);
        Assert.Equal(ValidadeDoVinculo.Prazo, formulario.Validade);
        Assert.Equal(new TempoDeVinculo(OpcaoDeTempo.Outro, 1, false), formulario.Tempo);
        Assert.Equal(Agora, formulario.Inicio);
    }

    [Fact]
    public void Vinculo_excluido_depois_de_montada_a_faixa_abre_em_branco()
    {
        var alvo = new AlvoDeVinculo("Word", "Contrato - Word", "", Em(17, 10, 0), Em(17, 10, 40), VinculoId: 7);

        var formulario = FormularioDeVinculo.Montar(alvo, [], Agora);

        Assert.Null(formulario.EmEdicao);
        Assert.Null(formulario.Alvo.VinculoId);
        Assert.Equal("", formulario.Escritorio);
        Assert.True(formulario.SoRegistro);
    }

    [Fact]
    public void Vinculo_aberto_pela_tela_de_vinculos_nao_oferece_so_o_registro()
    {
        var vinculo = Novo(2, "2200", AlcanceDoVinculo.Programa, "Excel", categoria: 1);

        var formulario = FormularioDeVinculo.DoVinculo(vinculo);

        Assert.Equal(2, formulario.EmEdicao);
        Assert.False(formulario.OfereceRegistro);
        Assert.False(formulario.PodeJanela);
        Assert.False(formulario.PodeAba);
        Assert.Equal(1, formulario.CategoriaId);
    }

    [Fact]
    public void Salvar_sem_mexer_em_nada_devolve_o_mesmo_vinculo()
    {
        var vinculo = Novo(
            7,
            "2200",
            AlcanceDoVinculo.Aba,
            "Google Chrome",
            "Sala - Google Chrome",
            "app.gather.town/app/sala",
            ValidadeDoVinculo.Inatividade,
            Em(17, 10, 3).AddSeconds(27),
            TimeSpan.FromMinutes(45),
            categoria: 4);
        var alvo = new AlvoDeVinculo("Google Chrome", "Outro nome - Google Chrome", "app.gather.town/app/sala", Em(17, 10, 3), Em(17, 10, 50), VinculoId: 7);
        var formulario = FormularioDeVinculo.Montar(alvo, [vinculo], Agora);
        var duracao = formulario.Tempo.EmHoras
            ? TimeSpan.FromHours(formulario.Tempo.Quantidade)
            : TimeSpan.FromMinutes(formulario.Tempo.Quantidade);

        var pedido = new PedidoDeVinculo(
            formulario.Escritorio,
            formulario.CategoriaId,
            formulario.Alcance,
            formulario.Alvo,
            formulario.Validade,
            formulario.Inicio,
            duracao,
            formulario.SoRegistro);
        var salvo = VinculoDeEscritorio.De(pedido, formulario.CategoriaId, vinculo.CriadoEm, vinculo.Id);

        Assert.Equal(vinculo, salvo);
    }

    [Theory]
    [InlineData(30, OpcaoDeTempo.MeiaHora, 0, false)]
    [InlineData(60, OpcaoDeTempo.UmaHora, 0, false)]
    [InlineData(1, OpcaoDeTempo.Outro, 1, false)]
    [InlineData(45, OpcaoDeTempo.Outro, 45, false)]
    [InlineData(90, OpcaoDeTempo.Outro, 90, false)]
    [InlineData(180, OpcaoDeTempo.Outro, 3, true)]
    public void Tempo_do_prazo_volta_para_a_opcao_em_que_foi_escolhido(int minutos, OpcaoDeTempo opcao, int quantidade, bool emHoras)
    {
        var tempo = TempoDeVinculo.De(TimeSpan.FromMinutes(minutos));

        Assert.Equal(new TempoDeVinculo(opcao, quantidade, emHoras), tempo);
    }

    [Fact]
    public void Sem_prazo_o_tempo_fica_na_primeira_opcao()
    {
        Assert.Equal(TempoDeVinculo.Padrao, TempoDeVinculo.De(null));
    }
}
