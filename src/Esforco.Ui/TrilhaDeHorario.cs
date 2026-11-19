using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Esforco.Ui;

public sealed class TrilhaDeHorario : Panel
{
    public const double AlturaDaHora = 120;
    private const double PisoMinutos = 10;
    private const double LarguraMinimaCompacta = 12;

    private const double AlturaTrilha = 54;
    private const double Gutter = 56;
    private const double FaixaDeHora = 18;

    private static readonly Pen Grade = CriarGrade();
    private static readonly Pen CanetaAgora = CriarCanetaAgora();
    private static readonly Brush PincelAgora = CriarPincelAgora();
    private static readonly Brush TintaHora = CriarTinta();
    private static readonly Typeface Fonte = new("Segoe UI");

    public TrilhaDeHorario()
    {
        ClipToBounds = true;
        UseLayoutRounding = false;
        SnapsToDevicePixels = false;
        Loaded += (_, _) =>
        {
            LerHorasDoDia();
            GarantirMarca();
        };
        DataContextChanged += (_, _) => LerHorasDoDia();
    }

    private MarcaDeAgora? _marca;

    public void InvalidarAgora()
    {
        if (MarcarAgora)
            _marca?.InvalidateVisual();
    }

    private void GarantirMarca()
    {
        if (!DiaInteiro || _marca is not null)
            return;

        var camada = AdornerLayer.GetAdornerLayer(this);
        if (camada is null)
            return;

        _marca = new MarcaDeAgora(this);
        camada.Add(_marca);
    }

    private void LerHorasDoDia()
    {
        if (DiaInteiro)
        {
            HoraInicial = 0;
            HoraFinal = 24;
            MarcarAgora = DiaAtual();
            return;
        }

        DependencyObject? atual = this;
        while (atual is not null)
        {
            if (atual is FrameworkElement elemento && elemento.DataContext is DiaCalendarioVm dia)
            {
                HoraInicial = dia.HoraInicial;
                HoraFinal = dia.HoraFinal;
                return;
            }

            atual = VisualTreeHelper.GetParent(atual);
        }
    }

    private bool DiaAtual()
    {
        DependencyObject? atual = this;
        while (atual is not null)
        {
            if (atual is FrameworkElement elemento && elemento.DataContext is DiaCalendarioVm dia)
                return dia.EhHoje;

            atual = VisualTreeHelper.GetParent(atual);
        }

        return false;
    }

    public int HoraInicial
    {
        get => (int)GetValue(HoraInicialProperty);
        set => SetValue(HoraInicialProperty, value);
    }

    public static readonly DependencyProperty HoraInicialProperty = DependencyProperty.Register(
        nameof(HoraInicial),
        typeof(int),
        typeof(TrilhaDeHorario),
        new FrameworkPropertyMetadata(
            8,
            FrameworkPropertyMetadataOptions.AffectsMeasure
            | FrameworkPropertyMetadataOptions.AffectsArrange
            | FrameworkPropertyMetadataOptions.AffectsRender));

    public int HoraFinal
    {
        get => (int)GetValue(HoraFinalProperty);
        set => SetValue(HoraFinalProperty, value);
    }

    public static readonly DependencyProperty HoraFinalProperty = DependencyProperty.Register(
        nameof(HoraFinal),
        typeof(int),
        typeof(TrilhaDeHorario),
        new FrameworkPropertyMetadata(
            18,
            FrameworkPropertyMetadataOptions.AffectsMeasure
            | FrameworkPropertyMetadataOptions.AffectsArrange
            | FrameworkPropertyMetadataOptions.AffectsRender));

    public bool Vertical
    {
        get => (bool)GetValue(VerticalProperty);
        set => SetValue(VerticalProperty, value);
    }

    public static readonly DependencyProperty VerticalProperty = DependencyProperty.Register(
        nameof(Vertical),
        typeof(bool),
        typeof(TrilhaDeHorario),
        new FrameworkPropertyMetadata(
            false,
            FrameworkPropertyMetadataOptions.AffectsMeasure
            | FrameworkPropertyMetadataOptions.AffectsArrange
            | FrameworkPropertyMetadataOptions.AffectsRender));

    public bool Compacto
    {
        get => (bool)GetValue(CompactoProperty);
        set => SetValue(CompactoProperty, value);
    }

    public static readonly DependencyProperty CompactoProperty = DependencyProperty.Register(
        nameof(Compacto),
        typeof(bool),
        typeof(TrilhaDeHorario),
        new FrameworkPropertyMetadata(
            false,
            FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));

    public bool Rotulos
    {
        get => (bool)GetValue(RotulosProperty);
        set => SetValue(RotulosProperty, value);
    }

    public static readonly DependencyProperty RotulosProperty = DependencyProperty.Register(
        nameof(Rotulos),
        typeof(bool),
        typeof(TrilhaDeHorario),
        new FrameworkPropertyMetadata(
            true,
            FrameworkPropertyMetadataOptions.AffectsMeasure
            | FrameworkPropertyMetadataOptions.AffectsArrange
            | FrameworkPropertyMetadataOptions.AffectsRender));

    public bool DiaInteiro
    {
        get => (bool)GetValue(DiaInteiroProperty);
        set => SetValue(DiaInteiroProperty, value);
    }

    public static readonly DependencyProperty DiaInteiroProperty = DependencyProperty.Register(
        nameof(DiaInteiro),
        typeof(bool),
        typeof(TrilhaDeHorario),
        new FrameworkPropertyMetadata(false, (origem, _) => ((TrilhaDeHorario)origem).LerHorasDoDia()));

    public bool MarcarAgora
    {
        get => (bool)GetValue(MarcarAgoraProperty);
        set => SetValue(MarcarAgoraProperty, value);
    }

    public static readonly DependencyProperty MarcarAgoraProperty = DependencyProperty.Register(
        nameof(MarcarAgora),
        typeof(bool),
        typeof(TrilhaDeHorario),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

    public static int GetInicioMinuto(DependencyObject elemento) => (int)elemento.GetValue(InicioMinutoProperty);

    public static void SetInicioMinuto(DependencyObject elemento, int valor) => elemento.SetValue(InicioMinutoProperty, valor);

    public static readonly DependencyProperty InicioMinutoProperty = DependencyProperty.RegisterAttached(
        "InicioMinuto",
        typeof(int),
        typeof(TrilhaDeHorario),
        new FrameworkPropertyMetadata(
            0,
            FrameworkPropertyMetadataOptions.AffectsParentMeasure | FrameworkPropertyMetadataOptions.AffectsParentArrange));

    public static int GetFimMinuto(DependencyObject elemento) => (int)elemento.GetValue(FimMinutoProperty);

    public static void SetFimMinuto(DependencyObject elemento, int valor) => elemento.SetValue(FimMinutoProperty, valor);

    public static int GetInicioSegundo(DependencyObject elemento) => (int)elemento.GetValue(InicioSegundoProperty);

    public static void SetInicioSegundo(DependencyObject elemento, int valor) => elemento.SetValue(InicioSegundoProperty, valor);

    public static readonly DependencyProperty InicioSegundoProperty = DependencyProperty.RegisterAttached(
        "InicioSegundo",
        typeof(int),
        typeof(TrilhaDeHorario),
        new FrameworkPropertyMetadata(
            0,
            FrameworkPropertyMetadataOptions.AffectsParentMeasure | FrameworkPropertyMetadataOptions.AffectsParentArrange));

    public static int GetFimSegundo(DependencyObject elemento) => (int)elemento.GetValue(FimSegundoProperty);

    public static void SetFimSegundo(DependencyObject elemento, int valor) => elemento.SetValue(FimSegundoProperty, valor);

    public static readonly DependencyProperty FimSegundoProperty = DependencyProperty.RegisterAttached(
        "FimSegundo",
        typeof(int),
        typeof(TrilhaDeHorario),
        new FrameworkPropertyMetadata(
            0,
            FrameworkPropertyMetadataOptions.AffectsParentMeasure | FrameworkPropertyMetadataOptions.AffectsParentArrange));

    public static readonly DependencyProperty FimMinutoProperty = DependencyProperty.RegisterAttached(
        "FimMinuto",
        typeof(int),
        typeof(TrilhaDeHorario),
        new FrameworkPropertyMetadata(
            0,
            FrameworkPropertyMetadataOptions.AffectsParentMeasure | FrameworkPropertyMetadataOptions.AffectsParentArrange));

    public static int GetTrilha(DependencyObject elemento) => (int)elemento.GetValue(TrilhaProperty);

    public static void SetTrilha(DependencyObject elemento, int valor) => elemento.SetValue(TrilhaProperty, valor);

    public static readonly DependencyProperty TrilhaProperty = DependencyProperty.RegisterAttached(
        "Trilha",
        typeof(int),
        typeof(TrilhaDeHorario),
        new FrameworkPropertyMetadata(
            0,
            FrameworkPropertyMetadataOptions.AffectsParentMeasure | FrameworkPropertyMetadataOptions.AffectsParentArrange));

    protected override Size MeasureOverride(Size available)
    {
        var horas = Math.Max(1, HoraFinal - HoraInicial);
        var largura = double.IsInfinity(available.Width) ? 640 : available.Width;

        if (Vertical)
        {
            var util = Math.Max(0, largura - (Rotulos ? Gutter : 0));
            foreach (UIElement filho in InternalChildren)
            {
                var altura = AlturaNoTempo(filho);
                filho.Measure(new Size(Math.Max(0, util - 4), altura));
            }

            return new Size(largura, horas * AlturaDaHora);
        }

        var trilhas = Trilhas();

        var alturaFaixa = Compacto ? trilhas * 8 + 2 : trilhas * AlturaTrilha + 4 + FaixaDeHora;
        if (InternalChildren.Count == 0)
            alturaFaixa = Compacto ? 0 : AlturaTrilha + FaixaDeHora;

        var span = horas * 60.0;
        foreach (UIElement filho in InternalChildren)
        {
            var minutos = Math.Max(1, GetFimMinuto(filho) - GetInicioMinuto(filho));
            var piso = Compacto ? LarguraMinimaCompacta : 12;
            var w = Math.Max(piso, minutos / span * largura);
            var h = Compacto ? 8 : AlturaTrilha - 8;
            filho.Measure(new Size(w, h));
        }

        return new Size(largura, alturaFaixa);
    }

    protected override Size ArrangeOverride(Size final)
    {
        var horas = Math.Max(1, HoraFinal - HoraInicial);
        var origem = HoraInicial * 60;

        if (Vertical)
        {
            var margem = Rotulos ? Gutter : 0;
            var util = Math.Max(0, final.Width - margem);
            var filhos = new List<UIElement>(InternalChildren.Count);
            foreach (UIElement filho in InternalChildren)
                filhos.Add(filho);

            filhos.Sort(CompararDoFundoParaAFrente);
            for (var i = 0; i < filhos.Count; i++)
                SetZIndex(filhos[i], i);

            foreach (var filho in filhos)
            {
                var atras = 0;
                foreach (var outro in filhos)
                {
                    if (ReferenceEquals(outro, filho))
                        break;
                    if (Sobrepoe(outro, filho))
                        atras++;
                }

                var recuo = Math.Min(atras * 16, Math.Max(0, util - 88));
                var inicio = InicioEmSegundos(filho);
                var y = (inicio - origem * 60) / 3600.0 * AlturaDaHora;
                filho.Arrange(new Rect(margem + recuo + 1, y, Math.Max(0, util - recuo - 3), AlturaNoTempo(filho)));
            }

            return final;
        }

        var span = horas * 60.0;
        var passo = Compacto ? 8.0 : AlturaTrilha;
        foreach (UIElement filho in InternalChildren)
        {
            var inicio = GetInicioMinuto(filho);
            var fim = Math.Max(inicio + 1, GetFimMinuto(filho));
            var x = (inicio - origem) / span * final.Width + 1;
            var piso = Compacto ? LarguraMinimaCompacta : 12;
            var largura = Math.Max(piso, (fim - inicio) / span * final.Width - 2);
            var y = GetTrilha(filho) * passo + 3 + (Compacto ? 0 : FaixaDeHora);
            var altura = Compacto ? 8 : AlturaTrilha - 8;
            filho.Arrange(new Rect(x, y, largura, altura));
        }

        return final;
    }

    protected override void OnRender(DrawingContext contexto)
    {
        if (Compacto || ActualWidth <= 0 || ActualHeight <= 0)
            return;

        var horas = Math.Max(1, HoraFinal - HoraInicial);
        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        if (Vertical)
        {
            var margem = Rotulos ? Gutter : 0;
            for (var hora = 0; hora <= horas; hora++)
            {
                var y = hora * AlturaDaHora;
                contexto.DrawLine(Grade, new Point(margem, y), new Point(ActualWidth, y));
                if (!Rotulos || hora == horas)
                    continue;

                var texto = new FormattedText(
                    $"{HoraInicial + hora:00}:00",
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    Fonte,
                    11,
                    TintaHora,
                    dpi);
                var esquerda = Math.Max(0, margem - texto.Width - 8);
                var topo = y - texto.Height / 2;
                if (topo < 0)
                    topo = 0;
                contexto.DrawText(texto, new Point(esquerda, topo));
            }

            if (MarcarAgora && _marca is null)
                DesenharAgora(contexto);

            return;
        }

        var passo = ActualWidth / horas;
        for (var hora = 0; hora <= horas; hora++)
        {
            var x = hora / (double)horas * ActualWidth;
            contexto.DrawLine(Grade, new Point(x, FaixaDeHora), new Point(x, ActualHeight));
            if (passo < 22 && hora % 2 != 0 && hora != horas)
                continue;

            var texto = new FormattedText(
                $"{HoraInicial + hora:00}",
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                Fonte,
                10,
                TintaHora,
                dpi);
            var esquerda = x;
            if (esquerda + texto.Width > ActualWidth)
                esquerda = Math.Max(0, ActualWidth - texto.Width);
            contexto.DrawText(texto, new Point(esquerda, 1));
        }
    }

    private static int InicioEmSegundos(UIElement elemento)
    {
        var segundos = GetInicioSegundo(elemento);
        var minutos = GetInicioMinuto(elemento);
        return segundos == 0 && minutos > 0 ? minutos * 60 : segundos;
    }

    private static int FimEmSegundos(UIElement elemento)
    {
        var segundos = GetFimSegundo(elemento);
        var minutos = GetFimMinuto(elemento);
        return segundos == 0 && minutos > 0 ? minutos * 60 : segundos;
    }

    private static double AlturaNoTempo(UIElement elemento)
    {
        var segundos = Math.Max(0, FimEmSegundos(elemento) - InicioEmSegundos(elemento));
        var altura = segundos / 3600.0 * AlturaDaHora;
        var piso = PisoMinutos / 60.0 * AlturaDaHora;
        return Math.Max(piso, altura);
    }

    private static int CompararDoFundoParaAFrente(UIElement a, UIElement b)
    {
        var duracao = DuracaoDe(b).CompareTo(DuracaoDe(a));
        if (duracao != 0)
            return duracao;

        var inicio = GetInicioMinuto(a).CompareTo(GetInicioMinuto(b));
        return inicio != 0 ? inicio : GetTrilha(a).CompareTo(GetTrilha(b));
    }

    private static int DuracaoDe(UIElement elemento) =>
        Math.Max(0, FimEmSegundos(elemento) - InicioEmSegundos(elemento));

    private static bool Sobrepoe(UIElement a, UIElement b)
    {
        var inicioA = InicioEmSegundos(a);
        var fimA = FimEmSegundos(a);
        var inicioB = InicioEmSegundos(b);
        var fimB = FimEmSegundos(b);
        return inicioA < fimB && inicioB < fimA;
    }

    private void DesenharAgora(DrawingContext contexto)
    {
        var agora = DateTime.Now;
        var segundo = agora.Hour * 3600 + agora.Minute * 60 + agora.Second;
        var y = (segundo - HoraInicial * 3600) / 3600.0 * AlturaDaHora;
        if (y < 0 || y > ActualHeight)
            return;

        contexto.DrawLine(CanetaAgora, new Point(0, y), new Point(ActualWidth, y));
        contexto.DrawEllipse(PincelAgora, null, new Point(4, y), 4, 4);
    }

    private int Trilhas()
    {
        var trilhas = 1;
        foreach (UIElement filho in InternalChildren)
            trilhas = Math.Max(trilhas, GetTrilha(filho) + 1);
        return trilhas;
    }

    private static Pen CriarGrade()
    {
        var pen = new Pen(new SolidColorBrush(Color.FromRgb(0xE6, 0xE1, 0xD6)), 1);
        pen.Freeze();
        return pen;
    }

    private sealed class MarcaDeAgora(TrilhaDeHorario painel) : Adorner(painel)
    {
        protected override void OnRender(DrawingContext contexto)
        {
            if (painel.MarcarAgora)
                painel.DesenharAgora(contexto);
        }
    }

    private static Pen CriarCanetaAgora()
    {
        var pen = new Pen(new SolidColorBrush(Color.FromRgb(0xC4, 0x45, 0x36)), 2);
        pen.Freeze();
        return pen;
    }

    private static Brush CriarPincelAgora()
    {
        var tinta = new SolidColorBrush(Color.FromRgb(0xC4, 0x45, 0x36));
        tinta.Freeze();
        return tinta;
    }

    private static Brush CriarTinta()
    {
        var tinta = new SolidColorBrush(Color.FromRgb(0x8A, 0x84, 0x7A));
        tinta.Freeze();
        return tinta;
    }
}
