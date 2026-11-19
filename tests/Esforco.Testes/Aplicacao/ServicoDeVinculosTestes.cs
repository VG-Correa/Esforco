using Esforco.Aplicacao;
using Esforco.Dominio;
using Esforco.Testes.Apoio;

namespace Esforco.Testes.Aplicacao;

public class ServicoDeVinculosTestes
{
    private const string Janelinha = "Vincular a um escritório";

    [Fact]
    public void Excluir_categoria_em_uso_pede_escolha_e_nao_apaga_nada()
    {
        var (_, vinculos, _, _, _) = Criar();
        var contratos = vinculos.CriarCategoria("Contratos").Categoria!;
        var criado = vinculos.Criar(Pedido("2200", "Word", categoria: contratos.Id)).Vinculo!;

        var plano = vinculos.ExcluirCategoria(contratos.Id);

        Assert.False(plano.Pode);
        Assert.Equal(1, plano.EmUso);
        Assert.Contains("1 vínculo", plano.Aviso);
        Assert.Equal(contratos, Assert.Single(vinculos.ListarCategorias()));
        Assert.Equal(contratos.Id, Assert.Single(vinculos.ListarVinculos()).CategoriaId);
        Assert.Equal(criado.Id, vinculos.ListarVinculos()[0].Id);
    }

    [Fact]
    public void Excluir_categoria_em_uso_trocando_por_outra_leva_os_vinculos_junto()
    {
        var (_, vinculos, _, _, _) = Criar();
        var contratos = vinculos.CriarCategoria("Contratos").Categoria!;
        var suporte = vinculos.CriarCategoria("Suporte").Categoria!;
        vinculos.Criar(Pedido("2200", "Word", categoria: contratos.Id));
        vinculos.Criar(Pedido("2200", "Excel", categoria: contratos.Id));
        vinculos.Criar(Pedido("2200", "Bloco de Notas"));

        var plano = vinculos.ExcluirCategoria(contratos.Id, EscolhaAoExcluirCategoria.Trocar(suporte.Id));

        Assert.True(plano.Pode);
        Assert.Equal(2, plano.EmUso);
        Assert.Equal(suporte, Assert.Single(vinculos.ListarCategorias()));
        Assert.Equal([suporte.Id, suporte.Id, null], vinculos.ListarVinculos().Select(item => item.CategoriaId).ToArray());
    }

    [Fact]
    public void Excluir_categoria_em_uso_retirando_deixa_os_vinculos_sem_categoria()
    {
        var (_, vinculos, _, _, _) = Criar();
        var contratos = vinculos.CriarCategoria("Contratos").Categoria!;
        vinculos.Criar(Pedido("2200", "Word", categoria: contratos.Id));

        var plano = vinculos.ExcluirCategoria(contratos.Id, EscolhaAoExcluirCategoria.Retirar);

        Assert.True(plano.Pode);
        Assert.Empty(vinculos.ListarCategorias());
        var vinculo = Assert.Single(vinculos.ListarVinculos());
        Assert.Null(vinculo.CategoriaId);
        Assert.Equal("2200", vinculo.Escritorio);
    }

    [Fact]
    public void Categoria_pode_ser_criada_renomeada_e_nao_repete_nome()
    {
        var (_, vinculos, _, _, _) = Criar();
        var contratos = vinculos.CriarCategoria("  Contratos ").Categoria!;

        Assert.Equal("Contratos", contratos.Nome);
        Assert.Equal("Essa categoria já existe.", vinculos.CriarCategoria("contratos").Aviso);
        Assert.Null(vinculos.RenomearCategoria(contratos.Id, "Contratos e aditivos"));
        Assert.Equal("Contratos e aditivos", Assert.Single(vinculos.ListarCategorias()).Nome);
    }

    [Fact]
    public void Categoria_escrita_na_janelinha_e_criada_junto_com_o_vinculo()
    {
        var (_, vinculos, _, _, _) = Criar();

        var primeiro = vinculos.Criar(Pedido("2200", "Word") with { NovaCategoria = "Contratos" }).Vinculo!;
        var segundo = vinculos.Criar(Pedido("2200", "Excel") with { NovaCategoria = "contratos" }).Vinculo!;

        var categoria = Assert.Single(vinculos.ListarCategorias());
        Assert.Equal(categoria.Id, primeiro.CategoriaId);
        Assert.Equal(categoria.Id, segundo.CategoriaId);
    }

    [Fact]
    public void Janelinha_nao_vira_registro_e_o_tempo_segue_na_janela_que_estava_na_frente()
    {
        var (servico, _, leitor, repositorio, relogio) = Criar();
        leitor.Proxima = new AmostraDeJanela("Excel", "custos.xlsx - Excel", false);
        servico.Iniciar();
        relogio.Instante = relogio.Instante.AddSeconds(5);

        var alvo = servico.SegurarJanelaDaFrente(Janelinha);
        leitor.Proxima = new AmostraDeJanela("Esforço", Janelinha, false, Propria: true);
        for (var i = 0; i < 20; i++)
        {
            relogio.Instante = relogio.Instante.AddSeconds(1);
            servico.Pulso();
        }

        Assert.Equal("Excel", alvo!.Programa);
        Assert.Equal("custos.xlsx - Excel", alvo.Titulo);
        var unico = Assert.Single(repositorio.Listar());
        Assert.Equal("Excel", unico.Programa);
        Assert.Null(unico.Fim);
        Assert.Equal(TimeSpan.FromSeconds(25), unico.DuracaoAte(relogio.Instante));
    }

    [Fact]
    public void Depois_de_solta_a_janela_do_esforco_volta_a_ser_registrada_como_antes()
    {
        var (servico, _, leitor, repositorio, relogio) = Criar();
        leitor.Proxima = new AmostraDeJanela("Excel", "custos.xlsx - Excel", false);
        servico.Iniciar();
        servico.SegurarJanelaDaFrente(Janelinha);
        servico.SoltarJanelaDaFrente();

        leitor.Proxima = new AmostraDeJanela("Esforço", "Esforço", false, Propria: true);
        relogio.Instante = relogio.Instante.AddSeconds(1);
        servico.Pulso();

        Assert.Equal(["Excel", "Esforço"], repositorio.Listar().Select(item => item.Programa).ToArray());
    }

    [Fact]
    public void Outra_janela_do_esforco_nao_e_confundida_com_a_janelinha()
    {
        var (servico, _, leitor, repositorio, relogio) = Criar();
        leitor.Proxima = new AmostraDeJanela("Excel", "custos.xlsx - Excel", false);
        servico.Iniciar();
        servico.SegurarJanelaDaFrente(Janelinha);

        leitor.Proxima = new AmostraDeJanela("Esforço", "Esforço", false, Propria: true);
        relogio.Instante = relogio.Instante.AddSeconds(1);
        servico.Pulso();

        Assert.Equal(2, repositorio.Listar().Count);
    }

    [Fact]
    public void Parado_o_atalho_nao_le_a_janela()
    {
        var (servico, _, leitor, repositorio, _) = Criar();
        leitor.Proxima = new AmostraDeJanela("Excel", "custos.xlsx - Excel", false);

        var alvo = servico.SegurarJanelaDaFrente(Janelinha);
        servico.Pulso();

        Assert.Null(alvo);
        Assert.Equal(0, leitor.Leituras);
        Assert.Empty(repositorio.Listar());
    }

    [Fact]
    public void Vinculo_pelo_atalho_vale_dali_em_diante_e_aparece_em_outros_trabalhos()
    {
        var (servico, _, leitor, repositorio, relogio) = Criar();
        repositorio.DefinirNomeDeEscritorio("2200", "NTM");
        leitor.Proxima = new AmostraDeJanela("Excel", "custos.xlsx - Excel", false);
        servico.Iniciar();
        Avancar(servico, relogio, 10);

        var alvo = servico.SegurarJanelaDaFrente(Janelinha)!;
        var resultado = servico.Vincular(new PedidoDeVinculo(
            "2200",
            null,
            AlcanceDoVinculo.Janela,
            alvo,
            ValidadeDoVinculo.Sempre,
            relogio.Instante,
            null,
            NovaCategoria: "Planilhas"));
        servico.SoltarJanelaDaFrente();
        Avancar(servico, relogio, 20);

        Assert.Null(resultado.Aviso);
        var linha = Assert.Single(servico.ConsultarEscritorios());
        Assert.Equal("NTM", linha.Rotulo);
        Assert.Equal(TimeSpan.FromMinutes(20), linha.DuracaoOutros);
        Assert.Equal("Planilhas", Assert.Single(linha.Outros!).Categoria);
        var gravado = Assert.Single(repositorio.Listar());
        Assert.Equal("", gravado.Escritorio);
        Assert.Equal(TimeSpan.FromMinutes(30), gravado.DuracaoAte(relogio.Instante));
    }

    [Fact]
    public void Lista_de_escritorios_traz_so_os_que_ja_tem_nome()
    {
        var (servico, _, _, repositorio, _) = Criar();
        repositorio.DefinirNomeDeEscritorio("2200", "NTM");
        repositorio.DefinirNomeDeEscritorio("1905", "Concha y Toro");
        repositorio.DefinirNomeDeEscritorio("3300", "");

        var nomes = servico.ListarEscritoriosComNome();

        Assert.Equal(["Concha y Toro", "NTM"], nomes.Select(item => item.Nome).ToArray());
    }

    [Fact]
    public void Endereco_da_aba_so_e_guardado_quando_existe_vinculo_por_endereco()
    {
        var (servico, vinculos, leitor, repositorio, relogio) = Criar();
        leitor.Proxima = new AmostraDeJanela("Google Chrome", "Sala - Google Chrome", false, "https://app.gather.town/app/sala?token=1");
        servico.Iniciar();

        Assert.Empty(vinculos.Contexto().Enderecos);

        var alvo = servico.SegurarJanelaDaFrente(Janelinha)!;
        servico.Vincular(new PedidoDeVinculo("2200", null, AlcanceDoVinculo.Aba, alvo, ValidadeDoVinculo.Sempre, relogio.Instante, null));
        servico.SoltarJanelaDaFrente();
        var primeiro = repositorio.Listar()[0].Id!.Value;

        leitor.Proxima = new AmostraDeJanela("Google Chrome", "Gmail - Google Chrome", false, "https://mail.google.com/mail/u/0/");
        Avancar(servico, relogio, 5);
        leitor.Proxima = new AmostraDeJanela("Google Chrome", "Outro nome de sala - Google Chrome", false, "https://app.gather.town/app/sala");
        Avancar(servico, relogio, 5);
        Avancar(servico, relogio, 10);

        var enderecos = vinculos.Contexto().Enderecos;
        var terceiro = repositorio.Listar()[2].Id!.Value;
        Assert.Equal(2, enderecos.Count);
        Assert.Equal("app.gather.town/app/sala", enderecos[primeiro]);
        Assert.Equal("app.gather.town/app/sala", enderecos[terceiro]);
        Assert.Equal(TimeSpan.FromMinutes(15), Assert.Single(servico.ConsultarEscritorios()).DuracaoOutros);
    }

    [Fact]
    public void Encerrar_agora_para_o_vinculo_e_guarda_o_que_ja_valeu()
    {
        var (servico, vinculos, leitor, _, relogio) = Criar();
        leitor.Proxima = new AmostraDeJanela("Excel", "custos.xlsx - Excel", false);
        servico.Iniciar();
        var criado = vinculos.Criar(Pedido("2200", "Excel") with { Inicio = relogio.Instante }).Vinculo!;
        Avancar(servico, relogio, 10);

        vinculos.EncerrarAgora(criado.Id);
        Avancar(servico, relogio, 30);

        Assert.Equal(TimeSpan.FromMinutes(10), Assert.Single(servico.ConsultarEscritorios()).DuracaoOutros);
        var salvo = Assert.Single(servico.ConsultarVinculos());
        Assert.False(salvo.Ativo);
        Assert.StartsWith("Encerrado em", salvo.Validade);
        Assert.Equal("Programa inteiro", salvo.Alcance);
    }

    [Fact]
    public void Editar_troca_escritorio_e_categoria_e_excluir_tira_do_relatorio()
    {
        var (servico, vinculos, leitor, _, relogio) = Criar();
        leitor.Proxima = new AmostraDeJanela("Excel", "custos.xlsx - Excel", false);
        servico.Iniciar();
        var criado = vinculos.Criar(Pedido("2200", "Excel") with { Inicio = relogio.Instante }).Vinculo!;
        Avancar(servico, relogio, 10);

        var editado = vinculos.Editar(criado.Id, Pedido("1905", "Excel") with { Inicio = criado.Inicio, NovaCategoria = "Custos" });

        Assert.Null(editado.Aviso);
        var linha = Assert.Single(servico.ConsultarEscritorios());
        Assert.Equal("1905", linha.Id);
        Assert.Equal("Custos", Assert.Single(linha.Outros!).Categoria);

        vinculos.Excluir(criado.Id);

        Assert.Empty(servico.ConsultarEscritorios());
    }

    [Fact]
    public void Resumo_mostra_o_escritorio_vinculado_no_registro()
    {
        var (servico, vinculos, leitor, repositorio, relogio) = Criar();
        repositorio.DefinirNomeDeEscritorio("2200", "NTM");
        leitor.Proxima = new AmostraDeJanela("Excel", "custos.xlsx - Excel", false);
        servico.Iniciar();
        vinculos.Criar(Pedido("2200", "Excel") with { Inicio = relogio.Instante });
        Avancar(servico, relogio, 10);

        servico.ConsultarResumo();

        Assert.Equal("NTM", Assert.Single(repositorio.Listar()).EscritorioVinculado);
    }

    [Fact]
    public void Atalho_padrao_e_ctrl_alt_e_e_a_troca_fica_guardada()
    {
        var (_, vinculos, _, _, _) = Criar();

        Assert.Equal("Ctrl+Alt+E", vinculos.LerAtalho().ToString());

        vinculos.DefinirAtalho(AtalhoDeTeclado.Criar(true, false, true, false, "f9")!);

        Assert.Equal("Ctrl+Shift+F9", vinculos.LerAtalho().ToString());
    }

    /// <summary>
    /// Passa o tempo com um pulso a cada dez segundos, como o programa faz. Um salto maior seria lido como ausência.
    /// </summary>
    private static void Avancar(ServicoDeEsforco servico, RelogioManual relogio, int minutos)
    {
        for (var i = 0; i < minutos * 6; i++)
        {
            relogio.Instante = relogio.Instante.AddSeconds(10);
            servico.Pulso();
        }
    }

    private static PedidoDeVinculo Pedido(string escritorio, string programa, long? categoria = null) =>
        new(
            escritorio,
            categoria,
            AlcanceDoVinculo.Programa,
            new AlvoDeVinculo(programa, ""),
            ValidadeDoVinculo.Sempre,
            new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.FromHours(-3)),
            null);

    private static (ServicoDeEsforco Servico, ServicoDeVinculos Vinculos, LeitorFalso Leitor, RepositorioEmMemoria Repositorio, RelogioManual Relogio) Criar()
    {
        var leitor = new LeitorFalso();
        var repositorio = new RepositorioEmMemoria();
        var relogio = new RelogioManual
        {
            Instante = new DateTimeOffset(2026, 11, 17, 9, 0, 0, TimeSpan.FromHours(-3)),
        };
        var vinculos = new ServicoDeVinculos(new RepositorioDeVinculosEmMemoria(), relogio);
        var servico = new ServicoDeEsforco(leitor, repositorio, new RegrasFixas(), relogio, vinculos);
        return (servico, vinculos, leitor, repositorio, relogio);
    }
}
