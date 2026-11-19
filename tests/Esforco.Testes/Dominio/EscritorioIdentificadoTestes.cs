using Esforco.Dominio;
using static Esforco.Testes.Apoio.Vinculos;

namespace Esforco.Testes.Dominio;

/// <summary>
/// A janelinha abre com o escritório que o Esforço já identificou sozinho, diz de onde ele veio
/// e não cria vínculo quando a pessoa só confirma o que já estava identificado.
/// </summary>
public class EscritorioIdentificadoTestes
{
    private const string ReuniaoDaStraumann = "Meet: Straumann LATAM <> SplitC - Google Chrome";
    private const string ReuniaoSemEscritorio = "Meet: Acme <> SplitC - Google Chrome";

    private static readonly DateTimeOffset Agora = Em(17, 15, 0);

    private static readonly NomesDePainel Nomes = new([
        new CadastroDeEscritorio("1759", "Straumann", ["Straumann LATAM"], []),
        new CadastroDeEscritorio("3155", "Galena V2", [], []),
    ]);

    private static readonly EscritorioIdentificado PelaReuniao = new("1759", FonteDoEscritorio.Reuniao);

    [Fact]
    public void Painel_e_identificado_pelo_endereco_mesmo_sem_nome_cadastrado()
    {
        var painel = Painel("1905", Em(17, 10, 0), Em(17, 10, 20));

        Assert.Equal(new EscritorioIdentificado("1905", FonteDoEscritorio.Painel), ResolvedorDeEscritorio.Identificar(painel, Nomes));
        Assert.True(ResolvedorDeEscritorio.Descoberto(painel, Nomes));
    }

    [Fact]
    public void Chatwoot_e_identificado_pelo_chat_aberto()
    {
        var chat = IntervaloDeEsforco.Abrir("Chatwoot", "Chatwoot - Google Chrome", "SplitC <> Straumann", "1759", false, Em(17, 10, 0));

        Assert.Equal(new EscritorioIdentificado("1759", FonteDoEscritorio.Chatwoot), ResolvedorDeEscritorio.Identificar(chat, Nomes));
        Assert.True(ResolvedorDeEscritorio.Descoberto(chat, Nomes));
    }

    [Fact]
    public void Reuniao_e_identificada_pelo_nome_ou_pela_variacao_do_escritorio()
    {
        var reuniao = Reuniao(ReuniaoDaStraumann, Em(17, 10, 0), Em(17, 10, 30));

        Assert.Equal(PelaReuniao, ResolvedorDeEscritorio.Identificar(reuniao, Nomes));
        Assert.True(ResolvedorDeEscritorio.Descoberto(reuniao, Nomes));
    }

    [Fact]
    public void Reuniao_cujo_titulo_nao_e_de_nenhum_escritorio_fica_sem_identificacao()
    {
        var reuniao = IntervaloDeEsforco.Abrir("Google Chrome", ReuniaoSemEscritorio, ReuniaoSemEscritorio, "Straumann", false, Em(17, 10, 0));

        Assert.Null(ResolvedorDeEscritorio.Identificar(reuniao, Nomes));
        Assert.False(ResolvedorDeEscritorio.Descoberto(reuniao, Nomes));
    }

    [Fact]
    public void Regra_do_titulo_identifica_o_escritorio_mas_nao_vence_o_vinculo()
    {
        var word = IntervaloDeEsforco.Abrir("Word", "Contrato - Word", "Contrato", "Straumann LATAM", false, Em(17, 10, 0));
        var semCadastro = IntervaloDeEsforco.Abrir("Word", "Contrato - Word", "Contrato", "Acme", false, Em(17, 10, 0));
        var semRegra = Janela("Word", "Contrato", Em(17, 10, 0), null);

        var achado = ResolvedorDeEscritorio.Identificar(word, Nomes);

        Assert.Equal(new EscritorioIdentificado("1759", FonteDoEscritorio.Regra), achado);
        Assert.False(achado!.Value.VenceVinculo);
        Assert.False(ResolvedorDeEscritorio.Descoberto(word, Nomes));
        Assert.Null(ResolvedorDeEscritorio.Identificar(semCadastro, Nomes));
        Assert.Null(ResolvedorDeEscritorio.Identificar(semRegra, Nomes));
    }

    [Fact]
    public void Pelo_atalho_a_janelinha_abre_com_o_escritorio_identificado_e_sem_vinculo_em_edicao()
    {
        var alvo = new AlvoDeVinculo("Google Chrome", ReuniaoDaStraumann, "meet.google.com/abc", Identificado: PelaReuniao);

        var formulario = FormularioDeVinculo.Montar(alvo, [], Agora);

        Assert.Equal("1759", formulario.Escritorio);
        Assert.Equal(PelaReuniao, formulario.Identificado);
        Assert.Null(formulario.EmEdicao);
        Assert.False(formulario.Copia);
        Assert.Null(formulario.CategoriaId);
        Assert.Equal(AlcanceDoVinculo.Aba, formulario.Alcance);
        Assert.Equal(ValidadeDoVinculo.Sempre, formulario.Validade);
        Assert.True(formulario.Alvo.Descoberto);
    }

    [Fact]
    public void Sem_escritorio_identificado_a_janelinha_continua_abrindo_em_branco()
    {
        var alvo = new AlvoDeVinculo("Google Chrome", ReuniaoSemEscritorio, "meet.google.com/abc");

        var formulario = FormularioDeVinculo.Montar(alvo, [], Agora);

        Assert.Equal("", formulario.Escritorio);
        Assert.Null(formulario.Identificado);
        Assert.Null(formulario.Aviso("", null, "", "", formulario.Alcance, false, formulario.Validade));
        Assert.False(formulario.SoRepeteOIdentificado("1759", "", formulario.Alcance, false, formulario.Validade));
    }

    [Fact]
    public void Salvar_sem_mexer_em_nada_so_repete_o_identificado_e_qualquer_mudanca_deixa_de_repetir()
    {
        var alvo = new AlvoDeVinculo("Google Chrome", ReuniaoDaStraumann, "meet.google.com/abc", Identificado: PelaReuniao);
        var formulario = FormularioDeVinculo.Montar(alvo, [], Agora);

        Assert.True(formulario.SoRepeteOIdentificado("1759", "", AlcanceDoVinculo.Aba, false, ValidadeDoVinculo.Sempre));
        Assert.True(formulario.SoRepeteOIdentificado("1759", "  ", AlcanceDoVinculo.Aba, false, ValidadeDoVinculo.Sempre));
        Assert.False(formulario.SoRepeteOIdentificado("3155", "", AlcanceDoVinculo.Aba, false, ValidadeDoVinculo.Sempre));
        Assert.False(formulario.SoRepeteOIdentificado("1759", "Reuniões", AlcanceDoVinculo.Aba, false, ValidadeDoVinculo.Sempre));
        Assert.False(formulario.SoRepeteOIdentificado("1759", "", AlcanceDoVinculo.Programa, false, ValidadeDoVinculo.Sempre));
        Assert.False(formulario.SoRepeteOIdentificado("1759", "", AlcanceDoVinculo.Aba, false, ValidadeDoVinculo.Prazo));
        Assert.False(formulario.SoRepeteOIdentificado(null, "", AlcanceDoVinculo.Aba, false, ValidadeDoVinculo.Sempre));
    }

    [Fact]
    public void Faixa_do_calendario_so_repete_enquanto_continua_marcada_como_so_o_registro()
    {
        var alvo = new AlvoDeVinculo("Google Chrome", ReuniaoDaStraumann, "", Em(17, 10, 0), Em(17, 10, 30), PelaReuniao);
        var formulario = FormularioDeVinculo.Montar(alvo, [], Agora, AlcanceDoVinculo.Programa);

        Assert.Equal("1759", formulario.Escritorio);
        Assert.True(formulario.SoRegistro);
        Assert.True(formulario.SoRepeteOIdentificado("1759", "", AlcanceDoVinculo.Programa, true, ValidadeDoVinculo.Sempre));
        Assert.False(formulario.SoRepeteOIdentificado("1759", "", AlcanceDoVinculo.Programa, false, ValidadeDoVinculo.Sempre));
        Assert.False(formulario.SoRepeteOIdentificado("3155", "", AlcanceDoVinculo.Programa, true, ValidadeDoVinculo.Sempre));
    }

    [Fact]
    public void Vinculo_salvo_aparece_no_lugar_do_escritorio_da_regra()
    {
        var vinculo = Novo(7, "3155", AlcanceDoVinculo.Janela, "Word", "Contrato - Word", categoria: 4);
        var alvo = new AlvoDeVinculo(
            "Word",
            "Contrato - Word",
            Identificado: new EscritorioIdentificado("1759", FonteDoEscritorio.Regra),
            VinculoId: 7);

        var formulario = FormularioDeVinculo.Montar(alvo, [vinculo], Agora);

        Assert.Equal(7, formulario.EmEdicao);
        Assert.Equal("3155", formulario.Escritorio);
        Assert.Null(formulario.Identificado);
        Assert.False(formulario.SoRepeteOIdentificado("1759", "", formulario.Alcance, false, formulario.Validade));
        Assert.Null(formulario.Aviso("Straumann (id 1759)", "3155", "Galena V2 (id 3155)", "", formulario.Alcance, false, formulario.Validade));
    }

    [Fact]
    public void A_tela_diz_de_onde_veio_o_escritorio_e_que_salvar_assim_nao_cria_vinculo()
    {
        var alvo = new AlvoDeVinculo("Google Chrome", ReuniaoDaStraumann, "meet.google.com/abc", Identificado: PelaReuniao);
        var formulario = FormularioDeVinculo.Montar(alvo, [], Agora);

        var aviso = formulario.Aviso("Straumann (id 1759)", "1759", "Straumann (id 1759)", "", AlcanceDoVinculo.Aba, false, ValidadeDoVinculo.Sempre)!;

        Assert.Equal("Identificado pelo Esforço, pelo título da reunião: Straumann (id 1759). Não é um vínculo salvo.", aviso.Origem);
        Assert.Equal(
            "Salvar assim não cria vínculo, porque o escritório já é este. A contagem do tempo é gravada se você mudou.",
            aviso.AoSalvar);
        Assert.False(aviso.Alerta);
    }

    [Fact]
    public void Trocar_o_escritorio_de_um_registro_identificado_avisa_que_o_registro_nao_muda()
    {
        var alvo = new AlvoDeVinculo("Google Chrome", ReuniaoDaStraumann, "meet.google.com/abc", Identificado: PelaReuniao);
        var formulario = FormularioDeVinculo.Montar(alvo, [], Agora);

        var aviso = formulario.Aviso("Straumann (id 1759)", "3155", "Galena V2 (id 3155)", "", AlcanceDoVinculo.Aba, false, ValidadeDoVinculo.Sempre)!;

        Assert.True(aviso.Alerta);
        Assert.Equal(
            "Este registro continua em Straumann (id 1759): o que o Esforço identifica pelo título da reunião vale mais que o vínculo. "
            + "O vínculo com Galena V2 (id 3155) só vai valer onde o Esforço não identificar o escritório sozinho.",
            aviso.AoSalvar);
        Assert.Contains("Não é um vínculo salvo", aviso.Origem);
    }

    [Fact]
    public void Mudar_outro_campo_mantendo_o_escritorio_identificado_avisa_que_o_vinculo_nao_muda_o_registro()
    {
        var alvo = new AlvoDeVinculo("Google Chrome", ReuniaoDaStraumann, "meet.google.com/abc", Identificado: PelaReuniao);
        var formulario = FormularioDeVinculo.Montar(alvo, [], Agora);

        var aviso = formulario.Aviso("Straumann (id 1759)", "1759", "Straumann (id 1759)", "Reuniões", AlcanceDoVinculo.Aba, false, ValidadeDoVinculo.Sempre)!;

        Assert.False(aviso.Alerta);
        Assert.Equal(
            "Salvar cria um vínculo, mas neste registro nada muda: o vínculo só vale onde o Esforço não identificar o escritório sozinho.",
            aviso.AoSalvar);
    }

    [Fact]
    public void Trocar_o_escritorio_que_veio_de_uma_regra_cria_vinculo_que_vale_mais_que_a_regra()
    {
        var alvo = new AlvoDeVinculo("Word", "Contrato - Word", Identificado: new EscritorioIdentificado("1759", FonteDoEscritorio.Regra));
        var formulario = FormularioDeVinculo.Montar(alvo, [], Agora);

        var igual = formulario.Aviso("Straumann (id 1759)", "1759", "Straumann (id 1759)", "", AlcanceDoVinculo.Janela, false, ValidadeDoVinculo.Sempre)!;
        var outro = formulario.Aviso("Straumann (id 1759)", "3155", "Galena V2 (id 3155)", "", AlcanceDoVinculo.Janela, false, ValidadeDoVinculo.Sempre)!;

        Assert.Equal("1759", formulario.Escritorio);
        Assert.False(formulario.Alvo.Descoberto);
        Assert.Equal("Identificado pelo Esforço, por uma regra do título: Straumann (id 1759). Não é um vínculo salvo.", igual.Origem);
        Assert.StartsWith("Salvar assim não cria vínculo", igual.AoSalvar);
        Assert.Equal("Salvar cria um vínculo, e o vínculo vale mais que a regra do título.", outro.AoSalvar);
        Assert.False(outro.Alerta);
    }

    [Fact]
    public void Sem_escritorio_marcado_a_tela_so_diz_de_onde_veio_o_identificado()
    {
        var alvo = new AlvoDeVinculo("Google Chrome", ReuniaoDaStraumann, "meet.google.com/abc", Identificado: PelaReuniao);
        var formulario = FormularioDeVinculo.Montar(alvo, [], Agora);

        var aviso = formulario.Aviso("Straumann (id 1759)", null, "", "", AlcanceDoVinculo.Aba, false, ValidadeDoVinculo.Sempre)!;

        Assert.Equal("", aviso.AoSalvar);
        Assert.False(aviso.Alerta);
    }

    [Theory]
    [InlineData(FonteDoEscritorio.Painel, "pelo endereço do Painel")]
    [InlineData(FonteDoEscritorio.Chatwoot, "pelo chat aberto no Chatwoot")]
    [InlineData(FonteDoEscritorio.Reuniao, "pelo título da reunião")]
    [InlineData(FonteDoEscritorio.Regra, "por uma regra do título")]
    public void Cada_fonte_tem_a_sua_frase(FonteDoEscritorio fonte, string frase)
    {
        Assert.Equal(frase, TextoDeVinculo.Fonte(fonte));
    }

    [Fact]
    public void Faixa_da_reuniao_identificada_leva_o_escritorio_para_a_janelinha()
    {
        var reuniao = Reuniao(ReuniaoDaStraumann, Em(17, 10, 0), Em(17, 10, 30));

        var alvo = Assert.Single(Dia([reuniao], ModoDoCalendario.Escritorios, null).Blocos).Alvo!;
        var formulario = FormularioDeVinculo.Montar(alvo, [], Agora);

        Assert.Equal(PelaReuniao, alvo.Identificado);
        Assert.True(alvo.Descoberto);
        Assert.Equal("1759", formulario.Escritorio);
        Assert.Equal(PelaReuniao, formulario.Identificado);
        Assert.True(formulario.SoRegistro);
    }

    [Fact]
    public void Faixa_do_painel_leva_o_escritorio_do_endereco_para_a_janelinha()
    {
        var painel = Painel("3155", Em(17, 10, 0), Em(17, 10, 20));

        var alvo = Assert.Single(Dia([painel], ModoDoCalendario.Programas, null).Blocos).Alvo!;

        Assert.Equal(new EscritorioIdentificado("3155", FonteDoEscritorio.Painel), alvo.Identificado);
        Assert.Equal("3155", FormularioDeVinculo.Montar(alvo, [], Agora).Escritorio);
    }

    [Fact]
    public void Faixa_de_reuniao_sem_escritorio_abre_a_janelinha_em_branco()
    {
        var reuniao = Reuniao(ReuniaoSemEscritorio, Em(17, 10, 0), Em(17, 10, 30));

        var alvo = Assert.Single(Dia([reuniao], ModoDoCalendario.Programas, null).Blocos).Alvo!;

        Assert.Null(alvo.Identificado);
        Assert.False(alvo.Descoberto);
        Assert.Equal("", FormularioDeVinculo.Montar(alvo, [], Agora).Escritorio);
    }

    [Fact]
    public void Faixa_que_junta_reuniao_e_janela_comum_continua_abrindo_pela_janela_que_aceita_vinculo()
    {
        var reuniao = Reuniao(ReuniaoDaStraumann, Em(17, 10, 0), Em(17, 10, 30));
        var gmail = Janela("Google Chrome", "Gmail", Em(17, 10, 30), Em(17, 10, 35));

        var alvo = Assert.Single(Dia([reuniao, gmail], ModoDoCalendario.Programas, null).Blocos).Alvo!;

        Assert.Equal("Gmail - Google Chrome", alvo.Titulo);
        Assert.Null(alvo.Identificado);
        Assert.Equal("", FormularioDeVinculo.Montar(alvo, [], Agora).Escritorio);
    }

    [Fact]
    public void Identificacao_nao_muda_quem_vence_no_relatorio()
    {
        var reuniao = Reuniao(ReuniaoDaStraumann, Em(17, 10, 0), Em(17, 10, 30));
        var word = IntervaloDeEsforco.Abrir("Word", "Contrato - Word", "Contrato", "Straumann", false, Em(17, 11, 0));
        word.Fechar(Em(17, 11, 30));
        var vinculos = Contexto([
            Novo(1, "3155", AlcanceDoVinculo.Programa, "Google Chrome"),
            Novo(2, "3155", AlcanceDoVinculo.Programa, "Word"),
        ]);

        var blocos = Dia([reuniao, word], ModoDoCalendario.Escritorios, vinculos).Blocos;

        Assert.Equal("Straumann", Assert.Single(blocos, bloco => bloco.InicioMinuto == 10 * 60).Titulo);
        Assert.Equal("Galena V2", Assert.Single(blocos, bloco => bloco.InicioMinuto == 11 * 60).Titulo);
    }

    private static DiaDeCalendario Dia(
        IEnumerable<IntervaloDeEsforco> intervalos,
        ModoDoCalendario modo,
        ContextoDeVinculos? vinculos)
    {
        var quadro = CalendarioDeEsforco.Montar(
            intervalos,
            new DateTime(2026, 11, 17),
            new DateTime(2026, 11, 17),
            Em(17, 18, 0),
            modo,
            Nomes,
            false,
            false,
            null,
            vinculos);
        return Assert.Single(quadro.Dias);
    }
}
