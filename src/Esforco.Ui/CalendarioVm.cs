using System.Windows.Input;
using System.Windows.Media;
using Esforco.Dominio;

namespace Esforco.Ui;

public sealed class DiaCalendarioVm : Observavel
{
    private string _duracao;

    public DiaCalendarioVm(DiaDeCalendario dia, int horaInicial, int horaFinal, Action<DateTime> abrir)
    {
        Rotulo = dia.Rotulo;
        Numero = dia.Numero.ToString();
        _duracao = dia.Duracao;
        NoPeriodo = dia.NoPeriodo;
        EhHoje = dia.EhHoje;
        Opacidade = dia.NoPeriodo ? 1 : 0.4;
        HoraInicial = horaInicial;
        HoraFinal = horaFinal;
        TemFaixa = dia.Blocos.Count > 0;
        Blocos = dia.Blocos.Select(bloco => new BlocoCalendarioVm(bloco)).ToList();
        Linhas = dia.Linhas.Select(linha => new LinhaCalendarioVm(linha)).ToList();
        Abrir = new Comando(() => abrir(dia.Dia), () => dia.NoPeriodo);
    }

    public string Rotulo { get; }

    public string Numero { get; }

    public string Duracao => _duracao;

    public void DefinirDuracao(string duracao)
    {
        if (_duracao == duracao)
            return;

        _duracao = duracao;
        Notificar(nameof(Duracao));
    }

    public bool NoPeriodo { get; }

    public bool EhHoje { get; }

    public double Opacidade { get; }

    public int HoraInicial { get; }

    public int HoraFinal { get; }

    public bool TemFaixa { get; }

    public IReadOnlyList<BlocoCalendarioVm> Blocos { get; }

    public IReadOnlyList<LinhaCalendarioVm> Linhas { get; }

    public ICommand Abrir { get; }
}

public sealed class BlocoCalendarioVm : Observavel
{
    private int _fim;
    private int _fimSegundo;
    private string _dica;

    public BlocoCalendarioVm(BlocoDeCalendario bloco)
    {
        Titulo = bloco.Titulo;
        Subtitulo = bloco.Subtitulo;
        Tipo = bloco.Tipo;
        Simbolo = SimboloDe(bloco.Tipo);
        _dica = bloco.Dica;
        InicioMinuto = bloco.InicioMinuto;
        _fim = bloco.FimMinuto;
        InicioSegundo = bloco.InicioSegundo;
        _fimSegundo = bloco.FimSegundo;
        Trilha = bloco.Trilha;
        Fundo = CoresDoCalendario.De(bloco.Chave);
    }

    public string Titulo { get; }

    public string Subtitulo { get; }

    public string Tipo { get; }

    public string Simbolo { get; }

    public bool TemTipo => Tipo.Length > 0;

    private static string SimboloDe(string tipo)
    {
        if (tipo.Length == 0)
            return "";

        var partes = tipo.Split(" · ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return string.Join(" ", partes.Select(Glifo));
    }

    private static string Glifo(string nome) => nome switch
    {
        "Painel" => "\uE9D9",
        "Chatwoot" => "\uE8F2",
        "Reunião" => "\uE714",
        _ => "",
    };

    public string Dica => _dica;

    public int InicioMinuto { get; }

    public int FimMinuto => _fim;

    public int InicioSegundo { get; }

    public int FimSegundo => _fimSegundo;

    public string Horario => $"{Hora(InicioSegundo)} – {Hora(_fimSegundo)}";

    private static string Hora(int segundo)
    {
        if (segundo >= 24 * 60 * 60)
            return "24:00:00";
        if (segundo < 0)
            segundo = 0;
        return $"{segundo / 3600:00}:{segundo / 60 % 60:00}:{segundo % 60:00}";
    }

    public int Trilha { get; }

    public Brush Fundo { get; }

    public void Atualizar(BlocoDeCalendario bloco)
    {
        if (_fim != bloco.FimMinuto || _fimSegundo != bloco.FimSegundo)
        {
            _fim = bloco.FimMinuto;
            _fimSegundo = bloco.FimSegundo;
            Notificar(nameof(FimMinuto));
            Notificar(nameof(FimSegundo));
            Notificar(nameof(Horario));
        }

        if (_dica == bloco.Dica)
            return;

        _dica = bloco.Dica;
        Notificar(nameof(Dica));
    }
}

public sealed class LinhaCalendarioVm
{
    public LinhaCalendarioVm(LinhaDoDia linha)
    {
        Titulo = linha.Titulo;
        Duracao = linha.Duracao;
        Fundo = CoresDoCalendario.De(linha.Chave);
    }

    public string Titulo { get; }

    public string Duracao { get; }

    public Brush Fundo { get; }
}

internal static class CoresDoCalendario
{
    private static readonly string[] Paleta =
    [
        "#1F3D32",
        "#2F5D7C",
        "#8A4B2F",
        "#5C4A72",
        "#3E6B4F",
        "#7A3E3E",
        "#245E63",
        "#6B5428",
        "#4A3F6B",
        "#6E3F55",
    ];

    private static readonly Dictionary<string, SolidColorBrush> Cache = new(StringComparer.Ordinal);

    public static SolidColorBrush De(string chave)
    {
        if (Cache.TryGetValue(chave, out var pincel))
            return pincel;

        var cor = (Color)ColorConverter.ConvertFromString(Paleta[Hash(chave) % Paleta.Length])!;
        pincel = new SolidColorBrush(cor);
        pincel.Freeze();
        Cache[chave] = pincel;
        return pincel;
    }

    private static int Hash(string texto)
    {
        var hash = 17;
        foreach (var caractere in texto)
            hash = unchecked(hash * 31 + caractere);
        return hash & 0x7fffffff;
    }
}
