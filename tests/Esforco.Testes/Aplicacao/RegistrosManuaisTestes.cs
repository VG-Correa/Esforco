using Esforco.Aplicacao;
using Esforco.Dominio;
using Esforco.Testes.Apoio;
using static Esforco.Testes.Apoio.Vinculos;

namespace Esforco.Testes.Aplicacao;

public class RegistrosManuaisTestes
{
    private static readonly DateTime Dia = new(2026, 11, 17);

    [Fact]
    public void Registro_criado_com_o_esforco_parado_entra_no_registro_no_calendario_e_em_escritorios()
    {
        var cenario = Criar();

        var resultado = cenario.Vinculos.CriarRegistroManual(Pedido("2200", Em(17, 7, 0), Em(17, 7, 30), "Suporte", "  Ligação com o cliente "));

        Assert.Null(resultado.Aviso);
        var registro = resultado.Registro!;
        Assert.True(registro.Id > 0);
        Assert.Equal("Ligação com o cliente", registro.Descricao);
        Assert.Equal(cenario.Relogio.Instante, registro.CriadoEm);
        Assert.False(cenario.Servico.EmLeitura);
        Assert.Equal(0, cenario.Leitor.Leituras);
        Assert.Empty(cenario.Repositorio.Listar());

        var resumo = cenario.Servico.ConsultarResumo(Dia, Dia);
        Assert.True(resumo.TemRegistros);
        var programa = Assert.Single(Assert.Single(resumo.Nos).Filhos);
        Assert.Equal(RegistroManual.NomeDoPrograma, programa.Rotulo);
        var tela = Assert.Single(programa.Filhos);
        Assert.Equal("Ligação com o cliente", tela.Rotulo);
        var linha = Assert.Single(tela.Intervalos);
        Assert.Equal(registro.Id, linha.Manual!.Id);
        Assert.Equal("Suporte", linha.Manual.Categoria);
        Assert.Equal("NTM", linha.EscritorioVinculado);
        Assert.Equal(TimeSpan.FromMinutes(30), programa.DuracaoTotal);

        var quadro = cenario.Servico.ConsultarCalendario(Dia, Dia, ModoDoCalendario.Escritorios, false, false);
        var bloco = Assert.Single(Assert.Single(quadro.Dias).Blocos);
        Assert.Equal(registro.Id, bloco.Manual);
        Assert.Equal("NTM", bloco.Titulo);
        Assert.Equal("Suporte · Ligação com o cliente", bloco.Subtitulo);

        var escritorio = Assert.Single(cenario.Servico.ConsultarEscritorios(Dia, Dia), item => item.Id == "2200");
        Assert.Equal([new OutroTrabalho("Suporte", TimeSpan.FromMinutes(30), TimeSpan.Zero)], escritorio.Outros);
        Assert.Equal(TimeSpan.FromMinutes(30), escritorio.DuracaoContada);
    }

    [Fact]
    public void Registro_no_futuro_e_aceito_e_ja_entra_nos_totais()
    {
        var cenario = Criar();

        var resultado = cenario.Vinculos.CriarRegistroManual(Pedido("2200", Em(17, 15, 0), Em(17, 16, 0)));

        Assert.Null(resultado.Aviso);
        var escritorio = Assert.Single(cenario.Servico.ConsultarEscritorios(Dia, Dia), item => item.Id == "2200");
        Assert.Equal(TimeSpan.FromMinutes(60), escritorio.DuracaoContada);
    }

    [Fact]
    public void Registro_que_passa_da_meia_noite_fica_inteiro_no_dia_em_que_comeca()
    {
        var cenario = Criar();
        cenario.Relogio.Instante = Em(19, 9, 0);
        cenario.Vinculos.CriarRegistroManual(Pedido("2200", Em(17, 23, 0), Em(18, 1, 0)));

        var noDia = Assert.Single(cenario.Servico.ConsultarEscritorios(Dia, Dia), item => item.Id == "2200");
        var noSeguinte = Assert.Single(cenario.Servico.ConsultarEscritorios(Dia.AddDays(1), Dia.AddDays(1)), item => item.Id == "2200");
        var calendario = cenario.Servico.ConsultarCalendario(Dia.AddDays(1), Dia.AddDays(1), ModoDoCalendario.Escritorios, false, false);

        Assert.Equal(TimeSpan.FromHours(2), noDia.DuracaoContada);
        Assert.Equal(TimeSpan.Zero, noSeguinte.DuracaoContada);
        var bloco = Assert.Single(Assert.Single(calendario.Dias).Blocos);
        Assert.Equal((0, 60), (bloco.InicioMinuto, bloco.FimMinuto));
    }

    [Fact]
    public void Categoria_escrita_e_criada_reaproveitada_ou_fica_vazia()
    {
        var cenario = Criar();

        var nova = cenario.Vinculos.CriarRegistroManual(Pedido("2200", Em(17, 7, 0), Em(17, 7, 30), " Suporte ")).Registro!;
        var mesma = cenario.Vinculos.CriarRegistroManual(Pedido("2200", Em(17, 8, 0), Em(17, 8, 30), "suporte")).Registro!;
        var vazia = cenario.Vinculos.CriarRegistroManual(Pedido("2200", Em(17, 9, 0), Em(17, 9, 30), "  ")).Registro!;

        var categoria = Assert.Single(cenario.Vinculos.ListarCategorias());
        Assert.Equal("Suporte", categoria.Nome);
        Assert.Equal(categoria.Id, nova.CategoriaId);
        Assert.Equal(categoria.Id, mesma.CategoriaId);
        Assert.Null(vazia.CategoriaId);
    }

    [Fact]
    public void Registro_sem_escritorio_com_fim_antes_do_inicio_ou_longo_demais_nao_e_gravado()
    {
        var cenario = Criar();

        Assert.Equal("Escolha o escritório.", cenario.Vinculos.CriarRegistroManual(Pedido("", Em(17, 7, 0), Em(17, 7, 30))).Aviso);
        Assert.Equal("O fim precisa ser depois do início.", cenario.Vinculos.CriarRegistroManual(Pedido("2200", Em(17, 7, 30), Em(17, 7, 0))).Aviso);
        Assert.Equal("Um registro manual tem até 24 horas.", cenario.Vinculos.CriarRegistroManual(Pedido("2200", Em(17, 7, 0), Em(18, 7, 30))).Aviso);
        Assert.Empty(cenario.Vinculos.RegistrosManuais());
        Assert.Empty(cenario.Vinculos.ListarCategorias());
    }

    [Fact]
    public void Registro_pode_ser_alterado_e_excluido_depois()
    {
        var cenario = Criar();
        var criado = cenario.Vinculos.CriarRegistroManual(Pedido("2200", Em(17, 7, 0), Em(17, 7, 30), "Suporte", "Ligação")).Registro!;
        cenario.Relogio.Instante = Em(17, 12, 0);

        var alterado = cenario.Vinculos.EditarRegistroManual(criado.Id, Pedido("3300", Em(17, 8, 0), Em(17, 9, 0), "", "Visita")).Registro!;

        Assert.Equal(criado.Id, alterado.Id);
        Assert.Equal(criado.CriadoEm, alterado.CriadoEm);
        Assert.Equal(Em(17, 12, 0), alterado.AlteradoEm);
        Assert.Equal(alterado, Assert.Single(cenario.Vinculos.RegistrosManuais()));
        Assert.Equal(alterado, cenario.Vinculos.RegistroManualDe(criado.Id));
        var linhas = cenario.Servico.ConsultarEscritorios(Dia, Dia);
        Assert.Equal(TimeSpan.Zero, Assert.Single(linhas, item => item.Id == "2200").DuracaoContada);
        Assert.Equal(TimeSpan.FromMinutes(60), Assert.Single(linhas, item => item.Id == "3300").DuracaoContada);
        Assert.Equal("Sem categoria", Assert.Single(Assert.Single(linhas, item => item.Id == "3300").Outros!).Categoria);

        cenario.Vinculos.ExcluirRegistroManual(criado.Id);

        Assert.Empty(cenario.Vinculos.RegistrosManuais());
        Assert.Null(cenario.Vinculos.RegistroManualDe(criado.Id));
        Assert.False(cenario.Servico.ConsultarResumo(Dia, Dia).TemRegistros);
        Assert.Equal("Esse registro não existe mais.", cenario.Vinculos.EditarRegistroManual(criado.Id, Pedido("2200", Em(17, 7, 0), Em(17, 7, 30))).Aviso);
    }

    [Fact]
    public void Lista_em_memoria_acompanha_os_registros_manuais_sem_reler_o_banco_a_cada_consulta()
    {
        var cenario = Criar();
        cenario.Vinculos.CriarRegistroManual(Pedido("2200", Em(17, 7, 0), Em(17, 7, 30)));
        for (var i = 0; i < 5; i++)
        {
            cenario.Servico.ConsultarResumo(Dia, Dia);
            cenario.Servico.ConsultarCalendario(Dia, Dia, ModoDoCalendario.Escritorios, false, false);
            cenario.Servico.ConsultarEscritorios(Dia, Dia);
        }

        var depoisDeCriar = cenario.Banco.LeiturasDeRegistrosManuais;
        cenario.Vinculos.CriarRegistroManual(Pedido("2200", Em(17, 8, 0), Em(17, 8, 30)));
        for (var i = 0; i < 5; i++)
            cenario.Servico.ConsultarEscritorios(Dia, Dia);

        Assert.Equal(1, depoisDeCriar);
        Assert.Equal(2, cenario.Banco.LeiturasDeRegistrosManuais);
        Assert.Equal(2, cenario.Vinculos.RegistrosManuais().Count);
        Assert.Equal(
            TimeSpan.FromMinutes(60),
            Assert.Single(cenario.Servico.ConsultarEscritorios(Dia, Dia), item => item.Id == "2200").DuracaoContada);
    }

    [Fact]
    public void Cada_consulta_recebe_intervalos_novos_do_registro_manual()
    {
        var cenario = Criar();
        cenario.Vinculos.CriarRegistroManual(Pedido("2200", Em(17, 7, 0), Em(17, 7, 30)));

        var primeiro = Assert.Single(cenario.Vinculos.IntervalosManuais());
        primeiro.MarcarDuranteReuniao(true);
        var segundo = Assert.Single(cenario.Vinculos.IntervalosManuais());

        Assert.NotSame(primeiro, segundo);
        Assert.False(segundo.DuranteReuniao);
    }

    [Fact]
    public void Zerar_periodo_tambem_tira_o_tempo_dos_registros_manuais()
    {
        var cenario = Criar();
        var dentro = cenario.Vinculos.CriarRegistroManual(Pedido("2200", Em(17, 7, 10), Em(17, 7, 20))).Registro!;
        var atravessa = cenario.Vinculos.CriarRegistroManual(Pedido("3300", Em(17, 6, 0), Em(17, 8, 0), "Suporte", "Visita")).Registro!;
        var fora = cenario.Vinculos.CriarRegistroManual(Pedido("2200", Em(17, 8, 0), Em(17, 8, 30))).Registro!;

        cenario.Servico.ZerarPeriodo(Em(17, 7, 0), Em(17, 7, 30));

        var registros = cenario.Vinculos.RegistrosManuais().OrderBy(item => item.Inicio).ToList();
        Assert.Equal(3, registros.Count);
        Assert.DoesNotContain(registros, item => item.Id == dentro.Id);
        Assert.Equal((atravessa.Id, Em(17, 6, 0), Em(17, 7, 0)), (registros[0].Id, registros[0].Inicio, registros[0].Fim));
        Assert.Equal((Em(17, 7, 30), Em(17, 8, 0)), (registros[1].Inicio, registros[1].Fim));
        Assert.NotEqual(atravessa.Id, registros[1].Id);
        Assert.Equal(("3300", atravessa.CategoriaId, "Visita"), (registros[1].Escritorio, registros[1].CategoriaId, registros[1].Descricao));
        Assert.Equal(fora, registros[2]);
        Assert.Equal(
            TimeSpan.FromMinutes(90),
            Assert.Single(cenario.Servico.ConsultarEscritorios(Dia, Dia), item => item.Id == "3300").DuracaoContada);
    }

    [Fact]
    public void Excluir_o_programa_registro_manual_exclui_todos_e_so_eles()
    {
        var cenario = Criar();
        var word = Janela("Word", "Contrato", Em(17, 6, 0), Em(17, 6, 30));
        cenario.Repositorio.Adicionar(word);
        cenario.Repositorio.SalvarFechamento(word);
        cenario.Vinculos.CriarRegistroManual(Pedido("2200", Em(17, 7, 0), Em(17, 7, 30)));
        cenario.Vinculos.CriarRegistroManual(Pedido("3300", Em(16, 7, 0), Em(16, 7, 30)));

        cenario.Servico.ExcluirPrograma("Word 2");
        Assert.Equal(2, cenario.Vinculos.RegistrosManuais().Count);

        cenario.Servico.ExcluirPrograma(RegistroManual.NomeDoPrograma);

        Assert.Empty(cenario.Vinculos.RegistrosManuais());
        Assert.Equal("Word", Assert.Single(cenario.Repositorio.Listar()).Programa);
    }

    [Fact]
    public void Excluir_categoria_usada_em_registro_manual_pede_escolha_e_depois_troca_ou_retira()
    {
        var cenario = Criar();
        var suporte = cenario.Vinculos.CriarCategoria("Suporte").Categoria!;
        var contratos = cenario.Vinculos.CriarCategoria("Contratos").Categoria!;
        cenario.Vinculos.CriarRegistroManual(Pedido("2200", Em(17, 7, 0), Em(17, 7, 30), "Suporte"));

        var semEscolha = cenario.Vinculos.ExcluirCategoria(suporte.Id);
        var troca = cenario.Vinculos.ExcluirCategoria(suporte.Id, EscolhaAoExcluirCategoria.Trocar(contratos.Id));

        Assert.False(semEscolha.Pode);
        Assert.Equal("1 registro manual", semEscolha.Onde);
        Assert.True(troca.Pode);
        Assert.Equal(contratos.Id, Assert.Single(cenario.Vinculos.RegistrosManuais()).CategoriaId);
        Assert.Equal("Contratos", Assert.Single(cenario.Vinculos.IntervalosManuais()).Manual!.Categoria);

        Assert.True(cenario.Vinculos.ExcluirCategoria(contratos.Id, EscolhaAoExcluirCategoria.Retirar).Pode);

        Assert.Null(Assert.Single(cenario.Vinculos.RegistrosManuais()).CategoriaId);
        Assert.Equal("", Assert.Single(cenario.Vinculos.IntervalosManuais()).Manual!.Categoria);
    }

    [Fact]
    public void Leitura_ligada_nao_mexe_no_registro_manual_e_ele_nao_vira_intervalo_gravado()
    {
        var cenario = Criar();
        cenario.Vinculos.CriarRegistroManual(Pedido("2200", Em(17, 8, 50), Em(17, 9, 10), "", "Ligação"));
        cenario.Leitor.Proxima = new AmostraDeJanela("Word", "Contrato - Word", false);

        cenario.Servico.Iniciar();
        cenario.Relogio.Instante = cenario.Relogio.Instante.AddSeconds(5);
        cenario.Servico.Pulso();
        cenario.Servico.Parar();

        var gravado = Assert.Single(cenario.Repositorio.Listar());
        Assert.Equal("Word", gravado.Programa);
        Assert.Null(gravado.Manual);
        var registro = Assert.Single(cenario.Vinculos.RegistrosManuais());
        Assert.Equal((Em(17, 8, 50), Em(17, 9, 10)), (registro.Inicio, registro.Fim));
        var programas = Assert.Single(cenario.Servico.ConsultarResumo(Dia, Dia).Nos).Filhos.Select(no => no.Rotulo).OrderBy(nome => nome).ToArray();
        Assert.Equal([RegistroManual.NomeDoPrograma, "Word"], programas);
    }

    private static PedidoDeRegistroManual Pedido(
        string escritorio,
        DateTimeOffset inicio,
        DateTimeOffset fim,
        string categoria = "",
        string descricao = "") =>
        new(escritorio, categoria, descricao, inicio, fim);

    private static (ServicoDeEsforco Servico, ServicoDeVinculos Vinculos, LeitorFalso Leitor, RepositorioEmMemoria Repositorio, RepositorioDeVinculosEmMemoria Banco, RelogioManual Relogio) Criar()
    {
        var leitor = new LeitorFalso();
        var repositorio = new RepositorioEmMemoria();
        repositorio.DefinirNomeDeEscritorio("2200", "NTM");
        repositorio.DefinirNomeDeEscritorio("3300", "VCT");
        var relogio = new RelogioManual { Instante = Em(17, 9, 0) };
        var banco = new RepositorioDeVinculosEmMemoria();
        var vinculos = new ServicoDeVinculos(banco, relogio);
        var servico = new ServicoDeEsforco(leitor, repositorio, new RegrasFixas(), relogio, vinculos);
        return (servico, vinculos, leitor, repositorio, banco, relogio);
    }
}
