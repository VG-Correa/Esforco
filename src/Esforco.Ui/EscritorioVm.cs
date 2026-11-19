using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Input;
using Esforco.Aplicacao;
using Esforco.Dominio;

namespace Esforco.Ui;

public sealed class EscritorioVm : Observavel
{
    private readonly ServicoDeEsforco _servico;
    private readonly Action _atualizar;
    private string _rotulo = "";
    private string _duracaoPainel = "00:00:00";
    private string _duracaoPainelNaReuniao = "";
    private string _duracaoChat = "00:00:00";
    private string _duracaoChatNaReuniao = "";
    private string _duracaoReunioes = "00:00:00";
    private string _duracaoOutros = "00:00:00";
    private string _duracaoOutrosNaReuniao = "";
    private string _duracaoTotal = "00:00:00";
    private string _parte = "0%";
    private string _nome = "";
    private string _nomeSalvo = "";
    private bool _nomeAlterado;
    private string _novaVariacao = "";
    private string _aviso = "";
    private bool _aberto;
    private bool _editavel;
    private string _editor = "";
    private string _assinaturaTemas = "";

    public EscritorioVm(ServicoDeEsforco servico, Action atualizar)
    {
        _servico = servico;
        _atualizar = atualizar;
        Alternar = new Comando(() => Aberto = !Aberto, () => true);
        SalvarNome = new Comando(AoSalvarNome, () => Editavel);
        AdicionarVariacao = new Comando(AoAdicionarVariacao, () => Editavel);
        EditarNome = new Comando(() => AbrirEditor("nome"), () => Editavel);
        EditarVariacoes = new Comando(() => AbrirEditor("variacoes"), () => Editavel);
        EditarPlanos = new Comando(() => AbrirEditor("planos"), () => Editavel);
        FecharEdicao = new Comando(() => DefinirEditor(""), () => Editando);
    }

    public string Chave { get; private set; } = "";

    public string Id { get; private set; } = "";

    public string Identificacao => Id.Length == 0 ? "" : $"Id {Id}";

    public bool Editavel
    {
        get => _editavel;
        private set => Definir(ref _editavel, value);
    }

    public string Rotulo
    {
        get => _rotulo;
        private set => Definir(ref _rotulo, value);
    }

    public string DuracaoPainel
    {
        get => _duracaoPainel;
        private set => Definir(ref _duracaoPainel, value);
    }

    public string DuracaoPainelNaReuniao
    {
        get => _duracaoPainelNaReuniao;
        private set
        {
            if (!DefinirRetornando(ref _duracaoPainelNaReuniao, value))
                return;

            Notificar(nameof(TemPainelNaReuniao));
        }
    }

    public bool TemPainelNaReuniao => _duracaoPainelNaReuniao.Length > 0;

    public string DuracaoChat
    {
        get => _duracaoChat;
        private set => Definir(ref _duracaoChat, value);
    }

    public string DuracaoChatNaReuniao
    {
        get => _duracaoChatNaReuniao;
        private set
        {
            if (!DefinirRetornando(ref _duracaoChatNaReuniao, value))
                return;

            Notificar(nameof(TemChatNaReuniao));
        }
    }

    public bool TemChatNaReuniao => _duracaoChatNaReuniao.Length > 0;

    public string DuracaoReunioes
    {
        get => _duracaoReunioes;
        private set => Definir(ref _duracaoReunioes, value);
    }

    public string DuracaoOutros
    {
        get => _duracaoOutros;
        private set => Definir(ref _duracaoOutros, value);
    }

    public string DuracaoOutrosNaReuniao
    {
        get => _duracaoOutrosNaReuniao;
        private set
        {
            if (!DefinirRetornando(ref _duracaoOutrosNaReuniao, value))
                return;

            Notificar(nameof(TemOutrosNaReuniao));
        }
    }

    public bool TemOutrosNaReuniao => _duracaoOutrosNaReuniao.Length > 0;

    public string DuracaoTotal
    {
        get => _duracaoTotal;
        private set => Definir(ref _duracaoTotal, value);
    }

    public string Parte
    {
        get => _parte;
        private set => Definir(ref _parte, value);
    }

    public string Nome
    {
        get => _nome;
        set
        {
            if (_nome == value)
                return;

            _nome = value;
            _nomeAlterado = NomesDePainel.Exibir(value) != _nomeSalvo;
            Notificar(nameof(Nome));
        }
    }

    public string NovaVariacao
    {
        get => _novaVariacao;
        set
        {
            if (!DefinirRetornando(ref _novaVariacao, value))
                return;

            if (_aviso.Length > 0)
                Aviso = "";
        }
    }

    public string Aviso
    {
        get => _aviso;
        private set
        {
            if (!DefinirRetornando(ref _aviso, value))
                return;

            Notificar(nameof(TemAviso));
        }
    }

    public bool TemAviso => _aviso.Length > 0;

    public bool Aberto
    {
        get => _aberto;
        private set
        {
            if (!DefinirRetornando(ref _aberto, value))
                return;

            if (!value)
                DefinirEditor("");

            Notificar(nameof(Seta));
            Notificar(nameof(ConteudoVisivel));
            Notificar(nameof(SemReunioes));
            Notificar(nameof(MostrarOutros));
            Notificar(nameof(MostrarTextoLivre));
        }
    }

    public bool Editando => _editor.Length > 0;

    public bool EditandoNome => _editor == "nome";

    public bool EditandoVariacoes => _editor == "variacoes";

    public bool EditandoPlanos => _editor == "planos";

    public string TextoVariacoes =>
        Variacoes.Count == 0 ? "" : string.Join(" • ", Variacoes.Select(item => item.Texto));

    public string TextoPlanos =>
        Planos.Count == 0 ? "" : string.Join(" • ", Planos.Select(item => item.Exibicao));

    public bool MostrarVariacoes => TemVariacoes && !EditandoVariacoes;

    public bool MostrarPlanos => TemPlanos && !EditandoPlanos;

    public string Seta => _aberto ? "\uE70D" : "\uE76C";

    public string Etiqueta =>
        Id.Length == 0 ? "sem id" : _nomeSalvo.Length > 0 ? $"Id {Id}" : "";

    public bool TemEtiqueta => Etiqueta.Length > 0;

    public bool ConteudoVisivel => _aberto;

    public bool SemReunioes => _aberto && Temas.Count == 0;

    public bool MostrarOutros => _aberto && Outros.Count > 0;

    public bool TemVariacoes => Variacoes.Count > 0;

    public bool TemPlanos => Planos.Count > 0;

    public bool SemPlanos => Editavel && Planos.Count == 0;

    public bool MostrarTextoLivre => Aberto && !Editavel;

    public string TextoLivre => "Este nome ainda não está ligado a um id.";

    public ObservableCollection<TemaReuniaoVm> Temas { get; } = new();

    public ObservableCollection<OutroTrabalhoVm> Outros { get; } = new();

    public ObservableCollection<VariacaoVm> Variacoes { get; } = new();

    public ObservableCollection<PlanoVm> Planos { get; } = new();

    public ICommand Alternar { get; }

    public ICommand SalvarNome { get; }

    public ICommand AdicionarVariacao { get; }

    public ICommand EditarNome { get; }

    public ICommand EditarVariacoes { get; }

    public ICommand EditarPlanos { get; }

    public ICommand FecharEdicao { get; }

    public void Aplicar(LinhaDeEscritorio linha, string parte)
    {
        Chave = linha.Chave;
        Id = linha.Id;
        Editavel = linha.Editavel;
        Rotulo = linha.Rotulo;
        DuracaoPainel = Formatar.Duracao(linha.DuracaoPainel);
        DuracaoPainelNaReuniao = linha.DuracaoPainelNaReuniao > TimeSpan.Zero
            ? Formatar.Duracao(linha.DuracaoPainelNaReuniao)
            : "";
        DuracaoChat = Formatar.Duracao(linha.DuracaoChat);
        DuracaoChatNaReuniao = linha.DuracaoChatNaReuniao > TimeSpan.Zero
            ? Formatar.Duracao(linha.DuracaoChatNaReuniao)
            : "";
        DuracaoReunioes = Formatar.Duracao(linha.DuracaoReunioes);
        DuracaoOutros = Formatar.Duracao(linha.DuracaoOutros);
        DuracaoOutrosNaReuniao = linha.DuracaoOutrosNaReuniao > TimeSpan.Zero
            ? Formatar.Duracao(linha.DuracaoOutrosNaReuniao)
            : "";
        DuracaoTotal = Formatar.Duracao(linha.DuracaoContada);
        Parte = parte;
        _nomeSalvo = linha.Nome;
        if (!_nomeAlterado)
            Nome = linha.Nome;

        SincronizarVariacoes(linha.Variacoes);
        SincronizarPlanos(linha.Planos);
        SincronizarTemas(linha.Temas);
        SincronizarOutros(linha.Outros ?? []);
        Notificar(nameof(MostrarOutros));
        Notificar(nameof(Identificacao));
        Notificar(nameof(Etiqueta));
        Notificar(nameof(TemEtiqueta));
        Notificar(nameof(TemVariacoes));
        Notificar(nameof(TemPlanos));
        Notificar(nameof(TextoVariacoes));
        Notificar(nameof(TextoPlanos));
        Notificar(nameof(MostrarVariacoes));
        Notificar(nameof(MostrarPlanos));
        Notificar(nameof(SemPlanos));
        Notificar(nameof(SemReunioes));
        Notificar(nameof(MostrarTextoLivre));
        CommandManager.InvalidateRequerySuggested();
    }

    public EstadoDoEscritorio Capturar() =>
        new(
            Aberto,
            Nome,
            _nomeAlterado,
            NovaVariacao,
            Planos.ToDictionary(plano => plano.Plano, plano => (plano.Nome, plano.Alterado), StringComparer.Ordinal),
            _editor);

    public void Restaurar(EstadoDoEscritorio estado)
    {
        Aberto = estado.Aberto;
        if (estado.Aberto)
            DefinirEditor(estado.Editor);
        NovaVariacao = estado.NovaVariacao;
        if (estado.NomeAlterado)
            Nome = estado.Nome;

        foreach (var plano in Planos)
        {
            if (estado.Planos.TryGetValue(plano.Plano, out var rascunho) && rascunho.Alterado)
                plano.Restaurar(rascunho.Nome);
        }
    }

    private void AoSalvarNome()
    {
        _servico.DefinirNomeDoEscritorio(Id, Nome);
        _nomeAlterado = false;
        DefinirEditor("");
        _atualizar();
    }

    private void AbrirEditor(string editor)
    {
        DefinirEditor(_editor == editor ? "" : editor);
        if (_editor.Length > 0)
            Aberto = true;
    }

    private void DefinirEditor(string editor)
    {
        if (_editor == editor)
            return;

        _editor = editor;
        Notificar(nameof(Editando));
        Notificar(nameof(EditandoNome));
        Notificar(nameof(EditandoVariacoes));
        Notificar(nameof(EditandoPlanos));
        Notificar(nameof(MostrarVariacoes));
        Notificar(nameof(MostrarPlanos));
        CommandManager.InvalidateRequerySuggested();
    }

    private void AoAdicionarVariacao()
    {
        var aviso = _servico.AdicionarVariacao(Id, NovaVariacao);
        Aviso = aviso ?? "";
        if (aviso is null)
            NovaVariacao = "";

        _atualizar();
    }

    private void RemoverVariacao(string texto)
    {
        _servico.RemoverVariacao(Id, texto);
        _atualizar();
    }

    private void SalvarPlano(string plano, string nome)
    {
        _servico.DefinirNomeDoPlano(Id, plano, nome);
        foreach (var item in Planos)
        {
            if (item.Plano == plano)
                item.Confirmar();
        }

        _atualizar();
    }

    private void SincronizarVariacoes(IReadOnlyList<string> variacoes)
    {
        if (Variacoes.Select(item => item.Texto).SequenceEqual(variacoes))
            return;

        Variacoes.Clear();
        foreach (var texto in variacoes)
        {
            var gravada = texto;
            Variacoes.Add(new VariacaoVm(gravada, () => RemoverVariacao(gravada)));
        }
    }

    private void SincronizarPlanos(IReadOnlyList<PlanoConhecido> planos)
    {
        var desejados = planos.Select(plano => plano.Plano).ToList();
        if (Planos.Select(plano => plano.Plano).SequenceEqual(desejados))
        {
            for (var i = 0; i < planos.Count; i++)
                Planos[i].Sincronizar(planos[i].Nome);
            return;
        }

        var rascunhos = Planos.ToDictionary(plano => plano.Plano, plano => (plano.Nome, plano.Alterado), StringComparer.Ordinal);
        Planos.Clear();
        foreach (var plano in planos)
        {
            var vm = new PlanoVm(plano.Plano, plano.Nome, nome => SalvarPlano(plano.Plano, nome));
            if (rascunhos.TryGetValue(plano.Plano, out var rascunho) && rascunho.Alterado)
                vm.Restaurar(rascunho.Nome);
            Planos.Add(vm);
        }
    }

    private void SincronizarTemas(IReadOnlyList<TemaDeReuniao> temas)
    {
        var assinatura = Assinar(temas);
        if (assinatura == _assinaturaTemas && Temas.Count == temas.Count)
        {
            for (var i = 0; i < temas.Count; i++)
                Temas[i].Atualizar(temas[i]);
            return;
        }

        _assinaturaTemas = assinatura;
        Temas.Clear();
        foreach (var tema in temas)
            Temas.Add(TemaReuniaoVm.Criar(tema));
    }

    private void SincronizarOutros(IReadOnlyList<OutroTrabalho> outros)
    {
        if (Outros.Select(item => item.Categoria).SequenceEqual(outros.Select(item => item.Categoria)))
        {
            for (var i = 0; i < outros.Count; i++)
                Outros[i].Atualizar(outros[i]);
            return;
        }

        Outros.Clear();
        foreach (var outro in outros)
            Outros.Add(new OutroTrabalhoVm(outro));
    }

    private static string Assinar(IReadOnlyList<TemaDeReuniao> temas)
    {
        var texto = new StringBuilder();
        foreach (var tema in temas)
        {
            texto.Append(tema.Tema).Append('|');
            foreach (var dia in tema.Dias)
            {
                texto.Append(dia.Rotulo).Append('|');
                foreach (var ocorrencia in dia.Ocorrencias)
                    texto.Append(ocorrencia.Inicio.UtcTicks).Append(';');
            }
        }

        return texto.ToString();
    }

    private bool DefinirRetornando<T>(ref T campo, T valor, [System.Runtime.CompilerServices.CallerMemberName] string? nome = null)
    {
        if (EqualityComparer<T>.Default.Equals(campo, valor))
            return false;

        campo = valor;
        Notificar(nome);
        return true;
    }
}

public sealed record EstadoDoEscritorio(
    bool Aberto,
    string Nome,
    bool NomeAlterado,
    string NovaVariacao,
    Dictionary<string, (string Nome, bool Alterado)> Planos,
    string Editor);

public sealed class VariacaoVm
{
    public VariacaoVm(string texto, Action remover)
    {
        Texto = texto;
        Remover = new Comando(remover, () => true);
    }

    public string Texto { get; }

    public ICommand Remover { get; }
}

public sealed class PlanoVm : Observavel
{
    private readonly Action<string> _salvar;
    private string _nome;
    private string _salvo;

    public PlanoVm(string plano, string nome, Action<string> salvar)
    {
        Plano = plano;
        Rotulo = $"Plano {plano}";
        _nome = nome;
        _salvo = nome;
        _salvar = salvar;
        Salvar = new Comando(() => _salvar(Nome), () => true);
    }

    public string Plano { get; }

    public string Rotulo { get; }

    public string Exibicao => string.IsNullOrWhiteSpace(_salvo) ? Plano : $"{_salvo} ({Plano})";

    public bool Alterado { get; private set; }

    public string Nome
    {
        get => _nome;
        set
        {
            if (_nome == value)
                return;

            _nome = value;
            Alterado = NomesDePainel.Exibir(value) != _salvo;
            Notificar(nameof(Nome));
        }
    }

    public ICommand Salvar { get; }

    public void Sincronizar(string salvo)
    {
        _salvo = salvo;
        if (!Alterado)
            Nome = salvo;
    }

    public void Restaurar(string nome) => Nome = nome;

    public void Confirmar() => Alterado = false;
}

/// <summary>
/// Uma categoria de Outros trabalhos dentro do cartão do escritório.
/// </summary>
public sealed class OutroTrabalhoVm : Observavel
{
    private string _duracao = "";
    private string _duracaoNaReuniao = "";

    public OutroTrabalhoVm(OutroTrabalho outro)
    {
        Categoria = outro.Categoria;
        Atualizar(outro);
    }

    public string Categoria { get; }

    public string Duracao
    {
        get => _duracao;
        private set => Definir(ref _duracao, value);
    }

    public string DuracaoNaReuniao
    {
        get => _duracaoNaReuniao;
        private set
        {
            if (_duracaoNaReuniao == value)
                return;

            _duracaoNaReuniao = value;
            Notificar(nameof(DuracaoNaReuniao));
            Notificar(nameof(TemNaReuniao));
        }
    }

    public bool TemNaReuniao => _duracaoNaReuniao.Length > 0;

    public void Atualizar(OutroTrabalho outro)
    {
        Duracao = Formatar.Duracao(outro.Duracao);
        DuracaoNaReuniao = outro.DuracaoNaReuniao > TimeSpan.Zero
            ? Formatar.Duracao(outro.DuracaoNaReuniao)
            : "";
    }
}

public sealed class TemaReuniaoVm : Observavel
{
    private string _duracao;

    private TemaReuniaoVm(string tema, string duracao)
    {
        Tema = tema;
        _duracao = duracao;
    }

    public string Tema { get; }

    public string Duracao
    {
        get => _duracao;
        private set => Definir(ref _duracao, value);
    }

    public ObservableCollection<DiaReuniaoVm> Dias { get; } = new();

    public bool VariosDias => Dias.Count > 1;

    public static TemaReuniaoVm Criar(TemaDeReuniao tema)
    {
        var vm = new TemaReuniaoVm(tema.Tema, Formatar.Duracao(tema.Duracao));
        var varios = tema.Dias.Count > 1;
        foreach (var dia in tema.Dias)
            vm.Dias.Add(DiaReuniaoVm.Criar(dia, varios ? dia.Rotulo : $"{tema.Tema} · {dia.Rotulo}"));
        return vm;
    }

    public void Atualizar(TemaDeReuniao tema)
    {
        Duracao = Formatar.Duracao(tema.Duracao);
        for (var i = 0; i < tema.Dias.Count; i++)
            Dias[i].Atualizar(tema.Dias[i]);
    }
}

public sealed class DiaReuniaoVm : Observavel
{
    private string _duracao;

    private DiaReuniaoVm(string rotulo, string titulo, string duracao)
    {
        Rotulo = rotulo;
        Titulo = titulo;
        _duracao = duracao;
    }

    public string Rotulo { get; }

    public string Titulo { get; }

    public bool Varias => Ocorrencias.Count > 1;

    public string FaixaUnica => Ocorrencias.Count == 1 ? "   " + Ocorrencias[0].Faixa : "";

    public string Duracao
    {
        get => _duracao;
        private set => Definir(ref _duracao, value);
    }

    public ObservableCollection<OcorrenciaVm> Ocorrencias { get; } = new();

    public static DiaReuniaoVm Criar(DiaDeReuniao dia, string titulo)
    {
        var vm = new DiaReuniaoVm(dia.Rotulo, titulo, Formatar.Duracao(dia.Duracao));
        foreach (var ocorrencia in dia.Ocorrencias)
            vm.Ocorrencias.Add(OcorrenciaVm.Criar(ocorrencia));
        return vm;
    }

    public void Atualizar(DiaDeReuniao dia)
    {
        Duracao = Formatar.Duracao(dia.Duracao);
        var antes = FaixaUnica;
        for (var i = 0; i < dia.Ocorrencias.Count; i++)
            Ocorrencias[i].Atualizar(dia.Ocorrencias[i]);
        if (antes != FaixaUnica)
            Notificar(nameof(FaixaUnica));
    }
}

public sealed class OcorrenciaVm : Observavel
{
    private string _faixa;
    private string _duracao;

    private OcorrenciaVm(string faixa, string duracao)
    {
        _faixa = faixa;
        _duracao = duracao;
    }

    public string Faixa
    {
        get => _faixa;
        private set => Definir(ref _faixa, value);
    }

    public string Duracao
    {
        get => _duracao;
        private set => Definir(ref _duracao, value);
    }

    public static OcorrenciaVm Criar(OcorrenciaDeReuniao ocorrencia) =>
        new(Formatar.Faixa(ocorrencia.Inicio, ocorrencia.Fim), Formatar.Duracao(ocorrencia.Duracao));

    public void Atualizar(OcorrenciaDeReuniao ocorrencia)
    {
        Faixa = Formatar.Faixa(ocorrencia.Inicio, ocorrencia.Fim);
        Duracao = Formatar.Duracao(ocorrencia.Duracao);
    }
}
