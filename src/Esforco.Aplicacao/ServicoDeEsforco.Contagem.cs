using Esforco.Dominio;

namespace Esforco.Aplicacao;

/// <summary>
/// A janela que conta enquanto estiver aberta segue a regra da reunião: sai da frente e continua
/// no mesmo registro, sem foco ou minimizada, e fecha quando a janela some, a leitura para,
/// o computador dorme ou a tela é bloqueada. Saber se está aberta é só consulta.
/// A aba de navegador que não está à mostra é procurada na lista de abas das janelas:
/// se está lá, segue contando; se saiu de lá, foi fechada e o registro termina onde foi visto.
/// </summary>
public sealed partial class ServicoDeEsforco
{
    private readonly Dictionary<long, DateTimeOffset> _vistas = new();

    /// <summary>
    /// Registros que as leituras das abas vêm dando como fechados, com o instante da primeira delas.
    /// </summary>
    private readonly Dictionary<long, DateTimeOffset> _sumidas = new();
    private bool _contaSemFoco;
    private bool _procurarNasAbas;
    private DateTimeOffset? _abasLidasEm;

    private sealed record Existentes(IReadOnlyList<JanelaAberta> Janelas, bool ComAbas);

    private sealed record Frente(
        string Programa,
        string Titulo,
        string Endereco,
        string Conversa,
        bool Minimizada,
        LocalDoPainel? Local,
        bool Pausado);

    private IReadOnlyList<ContagemDeJanela> RegrasDeContagem() => _vinculos?.Contagens() ?? [];

    private bool HaJanelaQueContaAberta() =>
        RegrasDeContagem().Any(regra => regra.Modo == ModoDeContagem.EnquantoAberta);

    private static bool ReuniaoQueContaAberta(IntervaloDeEsforco intervalo, IReadOnlyList<ContagemDeJanela> regras) =>
        InterpretadorDeReuniao.Ler(intervalo.Titulo) is not null
        && ContagemDaJanela.Achar(intervalo.Programa, intervalo.Titulo, regras).Modo == ModoDeContagem.EnquantoAberta;

    /// <summary>
    /// A regra que manda a janela contar enquanto aberta. A reunião não entra aqui: ela tem o caminho dela.
    /// </summary>
    private static ContagemDeJanela? RegraQueContaAberta(
        string programa,
        string titulo,
        IReadOnlyList<ContagemDeJanela> regras)
    {
        if (regras.Count == 0 || InterpretadorDeReuniao.Ler(titulo) is not null)
            return null;

        var efetiva = ContagemDaJanela.Achar(programa, titulo, regras);
        return efetiva.Modo == ModoDeContagem.EnquantoAberta ? efetiva.Regra : null;
    }

    /// <summary>
    /// O que estava na frente deixa de estar. Se conta enquanto aberto, o registro segue sem foco;
    /// se a frente nova é da mesma regra (outra tela do mesmo programa), ele fecha e a tela nova assume.
    /// </summary>
    private void SairDaFrente(
        IntervaloDeEsforco aberto,
        ContagemDeJanela? regraDaFrente,
        IReadOnlyList<ContagemDeJanela> regras,
        DateTimeOffset agora)
    {
        var regra = RegraQueContaAberta(aberto.Programa, aberto.Titulo, regras);
        if (regra is null || regra.Id == regraDaFrente?.Id || EstaPausado(aberto.Programa))
        {
            Fechar(aberto, agora);
            return;
        }

        _repositorio.DefinirSegundoPlano(aberto, true);
    }

    /// <summary>
    /// A janela que contava sem foco voltou para a frente: continua no mesmo registro.
    /// </summary>
    private bool VoltarParaAFrente(
        ContagemDeJanela regra,
        Frente frente,
        IReadOnlyList<ContagemDeJanela> regras,
        DateTimeOffset agora)
    {
        IntervaloDeEsforco? volta = null;
        foreach (var fundo in _repositorio.ListarEmSegundoPlano())
        {
            if (RegraQueContaAberta(fundo.Programa, fundo.Titulo, regras)?.Id != regra.Id)
                continue;

            if (volta is null && MesmaAtividade(fundo, frente))
                volta = fundo;
            else
                Fechar(fundo, agora);
        }

        if (volta is null)
            return false;

        _repositorio.DefinirSegundoPlano(volta, false);
        if (volta.Id is long id)
        {
            _repositorio.MarcarVisto(id, agora);
            _vistas[id] = agora;
        }

        return true;
    }

    /// <summary>
    /// Cuida do que conta sem estar na frente, fora a reunião. Sem a lista das janelas abertas
    /// (a leitura falhou neste pulso) nada é fechado por sumiço e nada novo é aberto.
    /// </summary>
    private void AcompanharAbertas(
        IReadOnlyList<ContagemDeJanela> regras,
        Existentes? existentes,
        string programaDaFrente,
        string tituloDaFrente,
        DateTimeOffset agora)
    {
        var abertas = existentes?.Janelas;
        var fundos = _repositorio.ListarEmSegundoPlano();
        if (fundos.Count == 0 && (abertas is null || regras.Count == 0))
            return;

        var contando = new HashSet<long>();
        var aberto = _repositorio.ObterAberto();
        if (aberto is not null && RegraQueContaAberta(aberto.Programa, aberto.Titulo, regras) is ContagemDeJanela daFrente)
            contando.Add(daFrente.Id);

        foreach (var fundo in fundos)
        {
            if (ReuniaoQueContaAberta(fundo, regras))
                continue;

            var regra = RegraQueContaAberta(fundo.Programa, fundo.Titulo, regras);
            if (regra is null || EstaPausado(fundo.Programa) || !contando.Add(regra.Id))
            {
                Fechar(fundo, agora);
                continue;
            }

            if (abertas is null)
                continue;

            // Fora do navegador, a janela que não aparece mais foi fechada. No navegador a aba pode
            // estar atrás de outra: a lista de abas diz se ela existe. Sem a lista, vale a folga da reunião.
            var vista = UltimaVista(fundo);
            var aberta = EstaAberta(fundo, regra, abertas);
            if (!aberta && !PodeEstarEscondida(fundo, abertas))
            {
                Fechar(fundo, vista);
                continue;
            }

            var presenca = aberta
                ? PresencaDeAba.Aberta
                : NasAbas(fundo, TituloNasAbas(fundo, regra), existentes, PodeEsconder(fundo), agora);
            if (presenca == PresencaDeAba.Aberta)
            {
                if (fundo.Id is long id)
                {
                    _repositorio.MarcarVisto(id, agora);
                    _vistas[id] = agora;
                    _sumidas.Remove(id);
                }

                continue;
            }

            if (Sumiu(fundo, presenca, vista, agora))
                Fechar(fundo, vista);
        }

        if (abertas is null)
            return;

        foreach (var regra in regras)
        {
            if (regra.Modo != ModoDeContagem.EnquantoAberta || contando.Contains(regra.Id))
                continue;

            var janela = abertas.FirstOrDefault(item =>
                !EhAFrente(item, programaDaFrente, tituloDaFrente) && ContaPelaRegra(item, regra, regras));
            if (janela is null)
                continue;

            var titulo = (janela.Titulo ?? "").Trim();
            var novo = IntervaloDeEsforco.Abrir(
                ProgramaDa(janela),
                titulo,
                Contexto(titulo, "", ""),
                Escritorio(titulo, "", ""),
                janela.Minimizada,
                agora,
                emSegundoPlano: true);
            _repositorio.Adicionar(novo);
            if (novo.Id is long criado)
                _vistas[criado] = agora;
            contando.Add(regra.Id);
        }
    }

    private DateTimeOffset UltimaVista(IntervaloDeEsforco intervalo) =>
        intervalo.Id is long id && _vistas.TryGetValue(id, out var vista) && vista > intervalo.Inicio
            ? vista
            : intervalo.Inicio;

    /// <summary>
    /// O painel e o Chatwoot ganham o nome pelo endereço, que só a janela da frente mostra.
    /// Sem foco, a aba deles é reconhecida pelo título, em qualquer janela de navegador.
    /// </summary>
    private static bool EstaAberta(IntervaloDeEsforco fundo, ContagemDeJanela regra, IReadOnlyList<JanelaAberta> abertas)
    {
        var abaDeNavegador = fundo.EhPainelSplitC || fundo.EhChatwoot;
        return abertas.Any(janela =>
        {
            var mesmoPrograma = ContagemDaJanela.Igual(ProgramaDa(janela), fundo.Programa);
            if (regra.Alcance == AlcanceDaContagem.Programa)
                return mesmoPrograma;

            return ContagemDaJanela.MesmoTitulo(janela.Titulo, fundo.Titulo)
                && (mesmoPrograma || (abaDeNavegador && janela.Navegador));
        });
    }

    private static bool PodeEstarEscondida(IntervaloDeEsforco fundo, IReadOnlyList<JanelaAberta> abertas) =>
        abertas.Any(PodeEsconder(fundo));

    /// <summary>
    /// As janelas em que a aba do registro pode estar atrás de outra aba.
    /// </summary>
    private static Func<JanelaAberta, bool> PodeEsconder(IntervaloDeEsforco fundo)
    {
        if (fundo.EhPainelSplitC || fundo.EhChatwoot)
            return janela => janela.Navegador;

        return janela => janela.Navegador && ContagemDaJanela.Igual(janela.Programa, fundo.Programa);
    }

    /// <summary>
    /// O texto que identifica o registro no nome de uma aba. Na regra do programa inteiro,
    /// qualquer aba do painel ou do Chatwoot serve, e elas são reconhecidas pelo nome do sistema.
    /// </summary>
    private static string TituloNasAbas(IntervaloDeEsforco fundo, ContagemDeJanela regra)
    {
        if (regra.Alcance != AlcanceDaContagem.Programa)
            return fundo.Titulo;

        if (fundo.EhChatwoot)
            return "Chatwoot";

        return fundo.EhPainelSplitC ? "SplitC" : fundo.Titulo;
    }

    /// <summary>
    /// Procura o registro nas abas das janelas. Nulo quando as abas não foram lidas neste pulso:
    /// aí vale o que a última leitura disse.
    /// </summary>
    private PresencaDeAba? NasAbas(
        IntervaloDeEsforco fundo,
        string titulo,
        Existentes? existentes,
        Func<JanelaAberta, bool> podeEsconder,
        DateTimeOffset agora)
    {
        if (existentes is null)
            return null;

        var janelas = existentes.Janelas.Where(podeEsconder).ToList();
        if (janelas.Count > 0)
        {
            _procurarNasAbas = true;
            if (!existentes.ComAbas)
                return null;
        }

        var presenca = AbasDoNavegador.Procurar(titulo, janelas);
        if (fundo.Id is long id)
        {
            if (presenca == PresencaDeAba.Fechada)
                _sumidas.TryAdd(id, agora);
            else
                _sumidas.Remove(id);
        }

        return presenca;
    }

    private bool EstaSumida(IntervaloDeEsforco fundo) => fundo.Id is long id && _sumidas.ContainsKey(id);

    /// <summary>
    /// Fechada nas abas, encerra quando as leituras dizem isso há mais tempo que a tolerância:
    /// uma leitura só não encerra nada. Sem saber das abas, espera a folga da reunião desde a última
    /// vez em que foi visto.
    /// </summary>
    private bool Sumiu(IntervaloDeEsforco fundo, PresencaDeAba? presenca, DateTimeOffset vista, DateTimeOffset agora)
    {
        if (fundo.Id is long id && _sumidas.TryGetValue(id, out var desde))
            return presenca == PresencaDeAba.Fechada && agora - desde >= AbasDoNavegador.Tolerancia;

        return agora - vista > ContinuidadeDeReuniao.Folga;
    }

    /// <summary>
    /// Quando a leitura para, a tela é bloqueada ou outra reunião entra, o que conta sem foco termina agora.
    /// O que já não aparecia em aba nenhuma termina onde foi visto pela última vez.
    /// </summary>
    private DateTimeOffset FimSemFoco(IntervaloDeEsforco fundo, DateTimeOffset agora)
    {
        if (!EstaSumida(fundo))
            return agora;

        var vista = UltimaVista(fundo);
        return vista < agora ? vista : agora;
    }

    private bool ContaPelaRegra(JanelaAberta janela, ContagemDeJanela regra, IReadOnlyList<ContagemDeJanela> regras)
    {
        var programa = ProgramaDa(janela);
        return regra.Casa(programa, janela.Titulo)
            && !EstaPausado(programa)
            && RegraQueContaAberta(programa, (janela.Titulo ?? "").Trim(), regras)?.Id == regra.Id;
    }

    /// <summary>
    /// A janela da frente já tem o registro dela, com o nome que a leitura da frente deu.
    /// </summary>
    private static bool EhAFrente(JanelaAberta janela, string programaDaFrente, string tituloDaFrente) =>
        ContagemDaJanela.Igual(janela.Programa, programaDaFrente)
        && string.Equals((janela.Titulo ?? "").Trim(), tituloDaFrente, StringComparison.Ordinal);

    private static string ProgramaDa(JanelaAberta janela) =>
        NomeDoPrograma(janela.Programa, null, (janela.Titulo ?? "").Trim(), "");
}
