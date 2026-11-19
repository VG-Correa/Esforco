using Esforco.Dominio;
using static Esforco.Testes.Apoio.Vinculos;

namespace Esforco.Testes.Dominio;

public class RegistroManualTestes
{
    [Theory]
    [InlineData("9", 9, 0)]
    [InlineData("09", 9, 0)]
    [InlineData("9:30", 9, 30)]
    [InlineData("09:05", 9, 5)]
    [InlineData("9h30", 9, 30)]
    [InlineData("9h", 9, 0)]
    [InlineData("9.30", 9, 30)]
    [InlineData("930", 9, 30)]
    [InlineData("0930", 9, 30)]
    [InlineData(" 14:45 ", 14, 45)]
    [InlineData("0:00", 0, 0)]
    [InlineData("23:59", 23, 59)]
    public void Hora_escrita_a_mao_e_entendida(string texto, int hora, int minuto)
    {
        Assert.Equal(new TimeSpan(hora, minuto, 0), HoraDoDia.Ler(texto));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("24:00")]
    [InlineData("12:60")]
    [InlineData("9:3")]
    [InlineData("abc")]
    [InlineData("12345")]
    [InlineData("-1")]
    [InlineData("9:30:15")]
    public void Hora_que_nao_existe_nao_e_aceita(string? texto)
    {
        Assert.Null(HoraDoDia.Ler(texto));
    }

    [Fact]
    public void Minuto_do_dia_vira_hora_escrita_e_a_meia_noite_do_fim_vira_zero_hora()
    {
        Assert.Equal("00:00", HoraDoDia.Escrever(0));
        Assert.Equal("09:05", HoraDoDia.Escrever(9 * 60 + 5));
        Assert.Equal("23:55", HoraDoDia.Escrever(23 * 60 + 55));
        Assert.Equal("00:00", HoraDoDia.Escrever(24 * 60));
    }

    [Theory]
    [InlineData(9 * 60 + 7, 9 * 60, 9 * 60 + 30)]
    [InlineData(9 * 60 + 15, 9 * 60 + 15, 9 * 60 + 45)]
    [InlineData(9 * 60 + 29.9, 9 * 60 + 15, 9 * 60 + 45)]
    [InlineData(0, 0, 30)]
    [InlineData(23 * 60 + 50, 23 * 60 + 30, 24 * 60)]
    [InlineData(-40, 0, 30)]
    [InlineData(5000, 23 * 60 + 30, 24 * 60)]
    public void Clique_comeca_no_quarto_de_hora_e_dura_meia_hora(double minuto, int inicio, int fim)
    {
        Assert.Equal((inicio, fim), SelecaoDeHorario.DoClique(minuto));
    }

    [Theory]
    [InlineData(9 * 60 + 2, 10 * 60 + 13, 9 * 60, 10 * 60 + 15)]
    [InlineData(10 * 60 + 13, 9 * 60 + 2, 9 * 60, 10 * 60 + 15)]
    [InlineData(9 * 60, 9 * 60 + 1, 9 * 60, 9 * 60 + 5)]
    [InlineData(23 * 60 + 58, 24 * 60 + 30, 23 * 60 + 55, 24 * 60)]
    [InlineData(-30, 20, 0, 20)]
    public void Arrasto_define_inicio_e_fim_de_cinco_em_cinco_minutos(double de, double ate, int inicio, int fim)
    {
        Assert.Equal((inicio, fim), SelecaoDeHorario.DoArrasto(de, ate));
    }

    [Fact]
    public void Botao_de_novo_registro_sugere_a_meia_hora_que_acabou_de_passar_ou_nove_horas_em_outro_dia()
    {
        var hoje = new DateTime(2026, 11, 19);

        Assert.Equal((16 * 60 + 25, 16 * 60 + 55), SelecaoDeHorario.Sugerir(hoje, hoje.AddHours(16).AddMinutes(58)));
        Assert.Equal((0, 30), SelecaoDeHorario.Sugerir(hoje, hoje.AddMinutes(12)));
        Assert.Equal((9 * 60, 9 * 60 + 30), SelecaoDeHorario.Sugerir(hoje.AddDays(-2), hoje.AddHours(16)));
    }

    [Fact]
    public void Registro_precisa_de_escritorio_de_fim_depois_do_inicio_e_de_ate_um_dia()
    {
        var bom = new PedidoDeRegistroManual("2200", "", "", Em(17, 10, 0), Em(17, 10, 30));

        Assert.Null(CadastroDeRegistrosManuais.Impedir(bom));
        Assert.Equal("Escolha o escritório.", CadastroDeRegistrosManuais.Impedir(bom with { Escritorio = "  " }));
        Assert.Equal("O fim precisa ser depois do início.", CadastroDeRegistrosManuais.Impedir(bom with { Fim = Em(17, 10, 0) }));
        Assert.Equal("O fim precisa ser depois do início.", CadastroDeRegistrosManuais.Impedir(bom with { Fim = Em(17, 9, 0) }));
        Assert.Null(CadastroDeRegistrosManuais.Impedir(bom with { Fim = Em(18, 10, 0) }));
        Assert.Equal("Um registro manual tem até 24 horas.", CadastroDeRegistrosManuais.Impedir(bom with { Fim = Em(18, 10, 1) }));
        Assert.Equal("A descrição tem até 200 letras.", CadastroDeRegistrosManuais.Impedir(bom with { Descricao = new string('a', 201) }));
        Assert.Null(CadastroDeRegistrosManuais.Impedir(bom with { Descricao = new string('a', 200) }));
    }

    [Fact]
    public void Dia_e_horas_da_tela_viram_o_periodo_do_registro()
    {
        var dia = new DateTime(2026, 11, 17);

        var periodo = CadastroDeRegistrosManuais.LerPeriodo(dia, "14:30", "15h", false);

        Assert.True(periodo.Valido);
        Assert.Equal(dia.AddHours(14).AddMinutes(30), periodo.Inicio!.Value.LocalDateTime);
        Assert.Equal(dia.AddHours(15), periodo.Fim!.Value.LocalDateTime);
    }

    [Fact]
    public void Fim_antes_do_inicio_so_passa_dizendo_que_termina_no_dia_seguinte()
    {
        var dia = new DateTime(2026, 11, 17);

        var semMarcar = CadastroDeRegistrosManuais.LerPeriodo(dia, "23:00", "01:00", false);
        var igual = CadastroDeRegistrosManuais.LerPeriodo(dia, "10:00", "10:00", false);
        var marcado = CadastroDeRegistrosManuais.LerPeriodo(dia, "23:00", "01:00", true);

        Assert.False(semMarcar.Valido);
        Assert.Contains("Termina no dia seguinte", semMarcar.Aviso);
        Assert.False(igual.Valido);
        Assert.True(marcado.Valido);
        Assert.Equal(TimeSpan.FromHours(2), marcado.Fim!.Value - marcado.Inicio!.Value);
        Assert.Equal(dia.AddDays(1).AddHours(1), marcado.Fim.Value.LocalDateTime);
    }

    [Fact]
    public void Periodo_sem_dia_com_hora_errada_ou_com_mais_de_um_dia_diz_o_que_falta()
    {
        var dia = new DateTime(2026, 11, 17);

        Assert.Equal("Escolha o dia.", CadastroDeRegistrosManuais.LerPeriodo(null, "10:00", "11:00", false).Aviso);
        Assert.Equal("Escreva o início como 14:30.", CadastroDeRegistrosManuais.LerPeriodo(dia, "dez", "11:00", false).Aviso);
        Assert.Equal("Escreva o fim como 15:00.", CadastroDeRegistrosManuais.LerPeriodo(dia, "10:00", "", false).Aviso);
        Assert.Equal("Um registro manual tem até 24 horas.", CadastroDeRegistrosManuais.LerPeriodo(dia, "10:00", "10:30", true).Aviso);
        Assert.True(CadastroDeRegistrosManuais.LerPeriodo(dia, "10:00", "10:00", true).Valido);
    }

    [Fact]
    public void Registro_no_futuro_e_registro_que_passa_da_meia_noite_sao_avisados_sem_impedir()
    {
        var agora = new DateTimeOffset(new DateTime(2026, 11, 17, 12, 0, 0, DateTimeKind.Local));

        var passado = CadastroDeRegistrosManuais.Notas(agora.AddHours(-3), agora.AddHours(-2), agora);
        var futuro = CadastroDeRegistrosManuais.Notas(agora.AddHours(1), agora.AddHours(2), agora);
        var emCurso = CadastroDeRegistrosManuais.Notas(agora.AddHours(-1), agora.AddHours(1), agora);
        var virada = CadastroDeRegistrosManuais.Notas(agora.AddHours(-14), agora.AddHours(-11), agora);
        var ateMeiaNoite = CadastroDeRegistrosManuais.Notas(agora.AddHours(-14), agora.AddHours(-12), agora);

        Assert.Empty(passado);
        Assert.Contains("ainda não chegou", Assert.Single(futuro));
        Assert.Contains("O fim ainda não chegou", Assert.Single(emCurso));
        Assert.Contains("Passa da meia-noite", Assert.Single(virada));
        Assert.Empty(ateMeiaNoite);
    }

    [Theory]
    [InlineData(5, "5 min")]
    [InlineData(45, "45 min")]
    [InlineData(60, "1 h")]
    [InlineData(90, "1 h 30 min")]
    [InlineData(24 * 60, "24 h")]
    public void Duracao_do_registro_e_escrita_em_horas_e_minutos(int minutos, string texto)
    {
        Assert.Equal(texto, CadastroDeRegistrosManuais.Duracao(TimeSpan.FromMinutes(minutos)));
    }

    [Fact]
    public void Zerar_um_periodo_exclui_encurta_ou_divide_os_registros_manuais()
    {
        var fora = Registro(1, "2200", Em(17, 8, 0), Em(17, 9, 0));
        var dentro = Registro(2, "2200", Em(17, 10, 15), Em(17, 10, 45));
        var comecaAntes = Registro(3, "2200", Em(17, 9, 30), Em(17, 10, 30));
        var terminaDepois = Registro(4, "2200", Em(17, 10, 30), Em(17, 11, 30));
        var atravessa = Registro(5, "3300", Em(17, 9, 0), Em(17, 12, 0), "Implantação", 7);
        var encostado = Registro(6, "2200", Em(17, 11, 0), Em(17, 11, 20));

        var corte = CadastroDeRegistrosManuais.Cortar(
            [fora, dentro, comecaAntes, terminaDepois, atravessa, encostado],
            Em(17, 10, 0),
            Em(17, 11, 0));

        Assert.Equal(new long[] { 2 }, corte.Excluir);
        Assert.Equal(
            [
                comecaAntes with { Fim = Em(17, 10, 0) },
                terminaDepois with { Inicio = Em(17, 11, 0) },
                atravessa with { Fim = Em(17, 10, 0) },
            ],
            corte.Atualizar);
        Assert.Equal([atravessa with { Id = 0, Inicio = Em(17, 11, 0) }], corte.Criar);
    }

    [Fact]
    public void Registro_manual_vira_intervalo_sem_titulo_e_com_id_que_nao_bate_com_intervalo_gravado()
    {
        var registro = Registro(7, " 2200 ", Em(17, 10, 0), Em(17, 10, 30), "NTM <> SplitC - Chatwoot", 3);

        var intervalo = IntervaloDeEsforco.DoRegistroManual(registro, "Suporte");

        Assert.Equal(-7L, intervalo.Id);
        Assert.Equal(RegistroManual.NomeDoPrograma, intervalo.Programa);
        Assert.Equal("", intervalo.Titulo);
        Assert.Equal("NTM <> SplitC - Chatwoot", intervalo.Contexto);
        Assert.Equal("2200", intervalo.Escritorio);
        Assert.Equal(Em(17, 10, 30), intervalo.Fim);
        Assert.False(intervalo.EhPainelSplitC);
        Assert.False(intervalo.EhChatwoot);
        Assert.Equal(new MarcaDeRegistroManual(7, 3, "Suporte", "NTM <> SplitC - Chatwoot"), intervalo.Manual);
        Assert.Equal(intervalo.Manual, intervalo.Recortar(Em(17, 10, 10), Em(17, 10, 20)).Manual);
        Assert.Equal(
            RegistroManual.SemDescricao,
            IntervaloDeEsforco.DoRegistroManual(registro with { Descricao = "  " }, null).Contexto);
    }

    [Fact]
    public void Vinculo_do_programa_nunca_pega_o_registro_manual()
    {
        var manual = Manual(1, "2200", Em(17, 10, 0), Em(17, 10, 30), descricao: "Ligação");
        var vinculo = Novo(1, "3300", AlcanceDoVinculo.Programa, RegistroManual.NomeDoPrograma);

        var trecho = Assert.Single(AplicadorDeVinculos.Aplicar([manual], Contexto([vinculo]), NomesDePainel.Vazio, Em(17, 12, 0)));

        Assert.False(vinculo.Casa(manual, ""));
        Assert.Equal(new VinculoAplicado(0, "2200", null, "", true), trecho.Vinculo);
        Assert.Same(manual, trecho.Intervalo);
    }

    [Fact]
    public void Excluir_categoria_conta_os_registros_manuais_que_a_usam()
    {
        var categorias = new[] { new Categoria(1, "Suporte"), new Categoria(2, "Contratos") };
        var vinculos = new[] { Novo(1, "2200", AlcanceDoVinculo.Programa, "Word", categoria: 1) };
        var registros = new[]
        {
            Registro(1, "2200", Em(17, 10, 0), Em(17, 10, 30), categoria: 1),
            Registro(2, "2200", Em(17, 11, 0), Em(17, 11, 30), categoria: 1),
            Registro(3, "2200", Em(17, 12, 0), Em(17, 12, 30), categoria: 2),
        };

        var doisTipos = CadastroDeCategorias.PlanejarExclusao(1, categorias, vinculos, null, registros);
        var soRegistro = CadastroDeCategorias.PlanejarExclusao(2, categorias, vinculos, null, registros);
        var comEscolha = CadastroDeCategorias.PlanejarExclusao(2, categorias, vinculos, EscolhaAoExcluirCategoria.Retirar, registros);

        Assert.False(doisTipos.Pode);
        Assert.Equal(3, doisTipos.EmUso);
        Assert.Equal("1 vínculo e 2 registros manuais", doisTipos.Onde);
        Assert.Contains("1 vínculo e 2 registros manuais", doisTipos.Aviso);
        Assert.False(soRegistro.Pode);
        Assert.Equal("1 registro manual", soRegistro.Onde);
        Assert.True(comEscolha.Pode);
        Assert.Equal(1, comEscolha.EmUso);
    }
}
