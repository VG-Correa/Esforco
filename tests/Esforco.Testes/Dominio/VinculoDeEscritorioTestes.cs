using Esforco.Dominio;
using Esforco.Testes.Apoio;
using static Esforco.Testes.Apoio.Vinculos;

namespace Esforco.Testes.Dominio;

public class VinculoDeEscritorioTestes
{
    private const string Chrome = "Google Chrome";

    [Fact]
    public void Aba_vence_janela_que_vence_programa()
    {
        var planilha = Janela(Chrome, "Planilha NTM", Em(17, 9, 0), Em(17, 10, 0));
        var programa = Novo(1, "100", AlcanceDoVinculo.Programa, Chrome);
        var janela = Novo(2, "200", AlcanceDoVinculo.Janela, Chrome, planilha.Titulo);
        var aba = Novo(3, "300", AlcanceDoVinculo.Aba, Chrome, planilha.Titulo);

        Assert.Equal("300", Dono(planilha, aba, programa, janela));
        Assert.Equal("200", Dono(planilha, programa, janela));
        Assert.Equal("100", Dono(planilha, programa));
    }

    [Fact]
    public void O_mais_especifico_vence_mesmo_quando_o_outro_e_mais_novo()
    {
        var planilha = Janela(Chrome, "Planilha NTM", Em(17, 9, 0), Em(17, 10, 0));
        var aba = Novo(1, "300", AlcanceDoVinculo.Aba, Chrome, planilha.Titulo);
        var programa = Novo(9, "100", AlcanceDoVinculo.Programa, Chrome);

        Assert.Equal("300", Dono(planilha, programa, aba));
    }

    [Fact]
    public void Temporario_vence_permanente_enquanto_vale()
    {
        var planilha = Janela(Chrome, "Planilha NTM", Em(17, 9, 0), Em(17, 10, 0));
        var permanente = Novo(1, "100", AlcanceDoVinculo.Programa, Chrome);
        var temporario = Novo(
            2,
            "200",
            AlcanceDoVinculo.Programa,
            Chrome,
            validade: ValidadeDoVinculo.Prazo,
            inicio: Em(17, 9, 0),
            duracao: TimeSpan.FromMinutes(30));

        var trechos = Aplicar([planilha], permanente, temporario);

        Assert.Equal(2, trechos.Count);
        Assert.Equal("200", trechos[0].Vinculo!.Escritorio);
        Assert.Equal(Em(17, 9, 0), trechos[0].Intervalo.Inicio);
        Assert.Equal(Em(17, 9, 30), trechos[0].Intervalo.Fim);
        Assert.Equal("100", trechos[1].Vinculo!.Escritorio);
        Assert.Equal(Em(17, 9, 30), trechos[1].Intervalo.Inicio);
        Assert.Equal(Em(17, 10, 0), trechos[1].Intervalo.Fim);
    }

    [Fact]
    public void Aba_permanente_vence_programa_temporario()
    {
        var planilha = Janela(Chrome, "Planilha NTM", Em(17, 9, 0), Em(17, 10, 0));
        var aba = Novo(1, "300", AlcanceDoVinculo.Aba, Chrome, planilha.Titulo);
        var temporario = Novo(
            2,
            "200",
            AlcanceDoVinculo.Programa,
            Chrome,
            validade: ValidadeDoVinculo.Prazo,
            inicio: Em(17, 9, 0),
            duracao: TimeSpan.FromHours(2));

        Assert.Equal("300", Dono(planilha, aba, temporario));
    }

    [Fact]
    public void Registro_marcado_no_calendario_vence_os_outros_vinculos()
    {
        var planilha = Janela(Chrome, "Planilha NTM", Em(17, 9, 0), Em(17, 10, 0));
        var aba = Novo(1, "300", AlcanceDoVinculo.Aba, Chrome, planilha.Titulo);
        var registro = Novo(
            2,
            "400",
            AlcanceDoVinculo.Programa,
            Chrome,
            validade: ValidadeDoVinculo.Prazo,
            inicio: Em(17, 9, 0),
            duracao: TimeSpan.FromHours(1),
            soRegistro: true);

        Assert.Equal("400", Dono(planilha, aba, registro));
    }

    [Fact]
    public void Entre_iguais_vence_o_mais_novo()
    {
        var planilha = Janela(Chrome, "Planilha NTM", Em(17, 9, 0), Em(17, 10, 0));
        var antigo = Novo(1, "100", AlcanceDoVinculo.Programa, Chrome);
        var novo = Novo(2, "200", AlcanceDoVinculo.Programa, Chrome);

        Assert.Equal("200", Dono(planilha, antigo, novo));
    }

    [Fact]
    public void O_que_o_programa_descobre_sozinho_vence_o_vinculo_manual()
    {
        var nomes = new NomesDePainel([new CadastroDeEscritorio("2200", "NTM", [], [])]);
        var painel = Painel("2200", Em(17, 9, 0), Em(17, 9, 30));
        var reuniao = Reuniao("NTM <> SplitC - Alinhamento", Em(17, 10, 0), Em(17, 11, 0));
        var semDono = Reuniao("Fulano <> SplitC - Conversa", Em(17, 11, 0), Em(17, 12, 0));
        var vinculos = Contexto(
        [
            Novo(1, "9999", AlcanceDoVinculo.Programa, painel.Programa),
            Novo(2, "9999", AlcanceDoVinculo.Programa, Chrome),
        ]);

        var trechos = AplicadorDeVinculos.Aplicar([painel, reuniao, semDono], vinculos, nomes, Em(17, 13, 0));

        Assert.Equal(3, trechos.Count);
        Assert.Null(trechos[0].Vinculo);
        Assert.Null(trechos[1].Vinculo);
        Assert.Equal("9999", trechos[2].Vinculo!.Escritorio);
    }

    [Fact]
    public void Vinculo_so_vale_dali_em_diante()
    {
        var antes = Janela("Word", "Contrato", Em(17, 8, 0), Em(17, 8, 30));
        var atravessa = Janela("Word", "Contrato", Em(17, 8, 50), Em(17, 9, 10));
        var vinculo = Novo(1, "100", AlcanceDoVinculo.Programa, "Word", inicio: Em(17, 9, 0));

        var trechos = Aplicar([antes, atravessa], vinculo);

        Assert.Equal(3, trechos.Count);
        Assert.Null(trechos[0].Vinculo);
        Assert.Same(antes, trechos[0].Intervalo);
        Assert.Null(trechos[1].Vinculo);
        Assert.Equal(Em(17, 9, 0), trechos[1].Intervalo.Fim);
        Assert.Equal("100", trechos[2].Vinculo!.Escritorio);
        Assert.Equal(Em(17, 9, 0), trechos[2].Intervalo.Inicio);
        Assert.Equal(Em(17, 9, 10), trechos[2].Intervalo.Fim);
        Assert.Equal(Em(17, 8, 50), atravessa.Inicio);
        Assert.Equal(Em(17, 9, 10), atravessa.Fim);
    }

    [Fact]
    public void Validade_por_prazo_termina_na_hora_marcada()
    {
        var vinculo = Novo(
            1,
            "100",
            AlcanceDoVinculo.Programa,
            "Word",
            validade: ValidadeDoVinculo.Prazo,
            inicio: Em(17, 9, 0),
            duracao: TimeSpan.FromMinutes(30));
        var antes = Janela("Word", "Contrato", Em(17, 8, 50), Em(17, 9, 10));
        var meio = Janela("Word", "Contrato", Em(17, 9, 20), Em(17, 9, 40));
        var depois = Janela("Word", "Contrato", Em(17, 10, 0), Em(17, 10, 10));

        var janela = ValidadeDeVinculo.Calcular(vinculo, [antes, meio, depois], null, Em(17, 12, 0));
        var trechos = Aplicar([antes, meio, depois], vinculo);

        Assert.Equal(Em(17, 9, 0), janela.Inicio);
        Assert.Equal(Em(17, 9, 30), janela.Fim);
        var vinculados = trechos.Where(trecho => trecho.Vinculo is not null).ToList();
        Assert.Equal(2, vinculados.Count);
        Assert.Equal((Em(17, 9, 0), Em(17, 9, 10)), (vinculados[0].Intervalo.Inicio, vinculados[0].Intervalo.Fim!.Value));
        Assert.Equal((Em(17, 9, 20), Em(17, 9, 30)), (vinculados[1].Intervalo.Inicio, vinculados[1].Intervalo.Fim!.Value));
        Assert.Equal(TimeSpan.FromMinutes(20), Soma(vinculados));
    }

    [Fact]
    public void Validade_por_inatividade_expira_quando_fica_tempo_demais_sem_voltar()
    {
        var vinculo = Novo(
            1,
            "100",
            AlcanceDoVinculo.Janela,
            "Word",
            "Contrato - Word",
            validade: ValidadeDoVinculo.Inatividade,
            inicio: Em(17, 9, 0),
            duracao: TimeSpan.FromMinutes(30));
        var primeira = Janela("Word", "Contrato", Em(17, 9, 0), Em(17, 9, 10));
        var pausa = Janela(Chrome, "Gmail", Em(17, 9, 10), Em(17, 9, 30));
        var segunda = Janela("Word", "Contrato", Em(17, 9, 30), Em(17, 9, 40));
        var longe = Janela(Chrome, "Gmail", Em(17, 9, 40), Em(17, 10, 20));
        var terceira = Janela("Word", "Contrato", Em(17, 10, 20), Em(17, 10, 30));
        IntervaloDeEsforco[] todos = [primeira, pausa, segunda, longe, terceira];

        var janela = ValidadeDeVinculo.Calcular(vinculo, todos, null, Em(17, 12, 0));
        var trechos = Aplicar(todos, vinculo);

        Assert.Equal(Em(17, 10, 10), janela.Fim);
        Assert.Equal("100", trechos.Single(trecho => trecho.Origem == primeira).Vinculo!.Escritorio);
        Assert.Equal("100", trechos.Single(trecho => trecho.Origem == segunda).Vinculo!.Escritorio);
        Assert.Null(trechos.Single(trecho => trecho.Origem == terceira).Vinculo);
        Assert.Null(trechos.Single(trecho => trecho.Origem == pausa).Vinculo);
    }

    [Fact]
    public void Validade_por_inatividade_segue_valendo_enquanto_a_pessoa_volta_a_tempo()
    {
        var vinculo = Novo(
            1,
            "100",
            AlcanceDoVinculo.Janela,
            "Word",
            "Contrato - Word",
            validade: ValidadeDoVinculo.Inatividade,
            inicio: Em(17, 9, 0),
            duracao: TimeSpan.FromMinutes(30));
        var primeira = Janela("Word", "Contrato", Em(17, 9, 0), Em(17, 9, 10));
        var segunda = Janela("Word", "Contrato", Em(17, 9, 35), null);

        var valendo = ValidadeDeVinculo.Calcular(vinculo, [primeira, segunda], null, Em(17, 11, 0));
        var semVoltar = ValidadeDeVinculo.Calcular(vinculo, [primeira], null, Em(17, 9, 30));
        var esquecido = ValidadeDeVinculo.Calcular(vinculo, [primeira], null, Em(17, 11, 0));

        Assert.Null(valendo.Fim);
        Assert.Null(semVoltar.Fim);
        Assert.Equal(Em(17, 9, 40), esquecido.Fim);
    }

    [Fact]
    public void Encerrar_agora_corta_a_validade_sem_mexer_no_que_ja_valeu()
    {
        var vinculo = Novo(1, "100", AlcanceDoVinculo.Programa, "Word", inicio: Em(17, 9, 0), encerradoEm: Em(17, 9, 30));
        var word = Janela("Word", "Contrato", Em(17, 9, 0), Em(17, 10, 0));

        var trechos = Aplicar([word], vinculo);

        Assert.Equal(2, trechos.Count);
        Assert.Equal("100", trechos[0].Vinculo!.Escritorio);
        Assert.Equal(Em(17, 9, 30), trechos[0].Intervalo.Fim);
        Assert.Null(trechos[1].Vinculo);
    }

    [Fact]
    public void Aba_casa_pelo_endereco_guardado_mesmo_com_outro_titulo()
    {
        var reuniao = Janela(Chrome, "Sala nova", Em(17, 9, 0), Em(17, 9, 30), id: 7);
        var outra = Janela(Chrome, "Sala nova", Em(17, 10, 0), Em(17, 10, 30), id: 8);
        var vinculo = Novo(1, "100", AlcanceDoVinculo.Aba, Chrome, "Sala antiga - Google Chrome", "app.gather.town/app/sala");
        var contexto = Contexto([vinculo], enderecos: new Dictionary<long, string> { [7] = "app.gather.town/app/sala" });

        var trechos = AplicadorDeVinculos.Aplicar([reuniao, outra], contexto, NomesDePainel.Vazio, Em(17, 12, 0));

        Assert.Equal("100", trechos[0].Vinculo!.Escritorio);
        Assert.Null(trechos[1].Vinculo);
    }

    [Fact]
    public void Endereco_fica_so_com_o_site_e_o_caminho()
    {
        Assert.Equal("teams.microsoft.com/v2", EnderecoDeAba.Normalizar("https://Teams.Microsoft.com/v2/?ctx=abc#/conversa"));
        Assert.Equal("app.gather.town/app/sala", EnderecoDeAba.Normalizar("app.gather.town/app/sala?token=segredo"));
        Assert.Equal("", EnderecoDeAba.Normalizar("   "));
    }

    [Fact]
    public void Pedido_sem_escritorio_ou_sem_prazo_e_recusado()
    {
        var alvo = new AlvoDeVinculo("Word", "Contrato - Word");
        var semEscritorio = new PedidoDeVinculo(" ", null, AlcanceDoVinculo.Programa, alvo, ValidadeDoVinculo.Sempre, Em(17, 9, 0), null);
        var semPrazo = new PedidoDeVinculo("100", null, AlcanceDoVinculo.Programa, alvo, ValidadeDoVinculo.Prazo, Em(17, 9, 0), null);
        var semTitulo = new PedidoDeVinculo(
            "100",
            null,
            AlcanceDoVinculo.Janela,
            new AlvoDeVinculo("Word", ""),
            ValidadeDoVinculo.Sempre,
            Em(17, 9, 0),
            null);
        var certo = new PedidoDeVinculo("100", null, AlcanceDoVinculo.Janela, alvo, ValidadeDoVinculo.Prazo, Em(17, 9, 0), TimeSpan.FromHours(1));

        Assert.Equal("Escolha o escritório.", VinculoDeEscritorio.Impedir(semEscritorio));
        Assert.Equal("Informe o prazo.", VinculoDeEscritorio.Impedir(semPrazo));
        Assert.NotNull(VinculoDeEscritorio.Impedir(semTitulo));
        Assert.Null(VinculoDeEscritorio.Impedir(certo));
    }

    [Fact]
    public void So_este_registro_vale_do_inicio_ao_fim_dele()
    {
        var alvo = new AlvoDeVinculo("Word", "Contrato - Word", "", Em(17, 9, 0), Em(17, 9, 45));
        var pedido = new PedidoDeVinculo("100", null, AlcanceDoVinculo.Janela, alvo, ValidadeDoVinculo.Sempre, Em(17, 12, 0), null, SoRegistro: true);

        var vinculo = VinculoDeEscritorio.De(pedido, null, Em(17, 12, 0));
        var janela = ValidadeDeVinculo.Calcular(vinculo, [], null, Em(17, 13, 0));

        Assert.True(vinculo.SoRegistro);
        Assert.Equal(Em(17, 9, 0), janela.Inicio);
        Assert.Equal(Em(17, 9, 45), janela.Fim);
        Assert.Equal("Contrato - Word", vinculo.Titulo);
    }

    private static string? Dono(IntervaloDeEsforco intervalo, params VinculoDeEscritorio[] vinculos) =>
        Assert.Single(Aplicar([intervalo], vinculos)).Vinculo?.Escritorio;

    private static IReadOnlyList<TrechoDeEsforco> Aplicar(
        IEnumerable<IntervaloDeEsforco> intervalos,
        params VinculoDeEscritorio[] vinculos) =>
        AplicadorDeVinculos.Aplicar(intervalos, Contexto(vinculos), NomesDePainel.Vazio, Em(17, 12, 0));

    private static TimeSpan Soma(IEnumerable<TrechoDeEsforco> trechos) =>
        trechos.Aggregate(TimeSpan.Zero, (total, trecho) => total + trecho.Intervalo.DuracaoAte(Em(17, 12, 0)));
}
