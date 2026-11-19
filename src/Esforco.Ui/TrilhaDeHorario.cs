using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Esforco.Dominio;

namespace Esforco.Ui;

public sealed class TrilhaDeHorario : Panel
{
    public const double AlturaDaHora = 120;
    private const double PisoMinutos = 10;
    private const double LarguraMinimaCompacta = 12;

    private const double AlturaTrilha = 54;
    private const double Gutter = 56;
    private const double FaixaDeHora = 18;

    /// <summary>
    /// Na coluna do dia, a beirada direita fica sempre sem faixa, para dar onde clicar e arrastar
    /// mesmo num horário que já tem registro.
    /// </summary>
    public const double BeiradaLivre = 12;

    private const double FolgaDoClique = 4;

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
    private BlocoCalendarioVm? _apertado;
    private double? _selecaoDe;
    private double _selecaoAte;
    private bool _arrastando;

    /// <summary>
    /// O clique numa faixa abre a janelinha de vínculo, ou a edição quando a faixa é um registro manual.
    /// O aperto fica marcado como tratado para que, na grade do mês, o botão do dia por baixo da faixa
    /// não abra o dia. Na coluna do dia, o aperto numa área vazia começa a escolha do horário de um
    /// registro manual: soltar no mesmo ponto é um clique, arrastar define início e fim.
    /// </summary>
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        _apertado = BlocoEm(e.OriginalSource);
        if (_apertado is not null)
        {
            e.Handled = true;
            return;
        }

        if (!ReferenceEquals(e.OriginalSource, this) || !ComecarSelecao(e.GetPosition(this).Y))
            return;

        e.Handled = true;
        if (!CaptureMouse())
            CancelarSelecao();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_selecaoDe is not null)
            MoverSelecao(e.GetPosition(this).Y);
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (_selecaoDe is not null)
        {
            e.Handled = true;
            MoverSelecao(e.GetPosition(this).Y);
            ConcluirSelecao();
            return;
        }

        var apertado = _apertado;
        _apertado = null;
        if (apertado is null)
            return;

        e.Handled = true;
        if (ReferenceEquals(BlocoEm(e.OriginalSource), apertado))
            apertado.Clicar();
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        _apertado = null;
    }

    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        base.OnLostMouseCapture(e);
        CancelarSelecao();
    }

    /// <summary>
    /// Só a coluna do dia, nas vistas diária e semanal, aceita criar registro pelo clique.
    /// </summary>
    private bool AceitaCriar => Vertical && DiaInteiro && !Rotulos && !Compacto;

    internal bool ComecarSelecao(double y)
    {
        if (!AceitaCriar || Dia() is not { PodeCriar: true })
            return false;

        _selecaoDe = y;
        _selecaoAte = y;
        _arrastando = false;
        RedesenharSelecao();
        return true;
    }

    internal void MoverSelecao(double y)
    {
        if (_selecaoDe is not double de)
            return;

        _selecaoAte = y;
        if (Math.Abs(y - de) >= FolgaDoClique)
            _arrastando = true;
        RedesenharSelecao();
    }

    internal void ConcluirSelecao()
    {
        var escolha = Selecao();
        var dia = Dia();
        CancelarSelecao();
        if (escolha is not (int inicio, int fim) || dia is null)
            return;

        // A janela do registro abre depois que o mouse terminou de ser solto.
        Dispatcher.BeginInvoke(() => dia.Criar(inicio, fim));
    }

    private void CancelarSelecao()
    {
        if (_selecaoDe is null)
            return;

        _selecaoDe = null;
        _arrastando = false;
        if (IsMouseCaptured)
            ReleaseMouseCapture();
        RedesenharSelecao();
    }

    /// <summary>
    /// O horário escolhido até aqui, em minutos do dia: meia hora a partir do clique,
    /// ou o trecho arrastado.
    /// </summary>
    internal (int Inicio, int Fim)? Selecao()
    {
        if (_selecaoDe is not double de)
            return null;

        return _arrastando
            ? SelecaoDeHorario.DoArrasto(MinutoEm(de), MinutoEm(_selecaoAte))
            : SelecaoDeHorario.DoClique(MinutoEm(de));
    }

    private double MinutoEm(double y) => HoraInicial * 60 + y / AlturaDaHora * 60;

    private void RedesenharSelecao()
    {
        if (_marca is not null)
            _marca.InvalidateVisual();
        else
            InvalidateVisual();
    }

    private DiaCalendarioVm? Dia()
    {
        DependencyObject? atual = this;
        while (atual is not null)
        {
            if (atual is FrameworkElement elemento && elemento.DataContext is DiaCalendarioVm dia)
                return dia;

            atual = VisualTreeHelper.GetParent(atual);
        }

        return null;
    }

    private static BlocoCalendarioVm? BlocoEm(object origem)
    {
        var bloco = origem switch
        {
            FrameworkElement elemento => elemento.DataContext as BlocoCalendarioVm,
            FrameworkContentElement conteudo => conteudo.DataContext as BlocoCalendarioVm,
            _ => null,
        };
        return bloco is { Clicavel: true } ? bloco : null;
    }

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

    private bool DiaAtual() => Dia() is { EhHoje: true };

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

    public static bool GetManual(DependencyObject elemento) => (bool)elemento.GetValue(ManualProperty);

    public static void SetManual(DependencyObject elemento, bool valor) => elemento.SetValue(ManualProperty, valor);

    /// <summary>
    /// Marca a faixa de um registro manual, que fica ao lado da faixa automática do mesmo horário.
    /// </summary>
    public static readonly DependencyProperty ManualProperty = DependencyProperty.RegisterAttached(
        "Manual",
        typeof(bool),
        typeof(TrilhaDeHorario),
        new FrameworkPropertyMetadata(
            false,
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
            var util = Math.Max(0, largura - (Rotulos ? Gutter : 0) - (AceitaCriar ? BeiradaLivre : 0));
            var filhos = DoFundoParaAFrente();
            var lugares = Lugares(filhos, util);
            for (var i = 0; i < filhos.Count; i++)
                filhos[i].Measure(new Size(Math.Max(0, lugares[i].Espaco - 4), AlturaNoTempo(filhos[i])));

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
            var util = Math.Max(0, final.Width - margem - (AceitaCriar ? BeiradaLivre : 0));
            var filhos = DoFundoParaAFrente();
            for (var i = 0; i < filhos.Count; i++)
                SetZIndex(filhos[i], i);

            var lugares = Lugares(filhos, util);
            for (var i = 0; i < filhos.Count; i++)
            {
                var filho = filhos[i];
                var inicio = InicioEmSegundos(filho);
                var y = (inicio - origem * 60) / 3600.0 * AlturaDaHora;
                filho.Arrange(new Rect(margem + lugares[i].X + 1, y, Math.Max(0, lugares[i].Largura - 3), AlturaNoTempo(filho)));
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
            // Sem um fundo, a área vazia não recebe o clique que cria o registro manual.
            if (AceitaCriar)
                contexto.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, ActualWidth, ActualHeight));

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

            if (_marca is null)
            {
                if (MarcarAgora)
                    DesenharAgora(contexto);
                DesenharSelecao(contexto);
            }

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

    private List<UIElement> DoFundoParaAFrente()
    {
        var filhos = new List<UIElement>(InternalChildren.Count);
        foreach (UIElement filho in InternalChildren)
            filhos.Add(filho);

        filhos.Sort(CompararDoFundoParaAFrente);
        return filhos;
    }

    /// <summary>
    /// A cascata das faixas que coincidem, e o registro manual ao lado da faixa automática do mesmo horário.
    /// </summary>
    private static IReadOnlyList<LugarDaFaixa> Lugares(List<UIElement> doFundoParaAFrente, double util)
    {
        var faixas = new List<FaixaNaColuna>(doFundoParaAFrente.Count);
        foreach (var filho in doFundoParaAFrente)
            faixas.Add(new FaixaNaColuna(InicioEmSegundos(filho), FimEmSegundos(filho), GetManual(filho)));

        return ColunaDoCalendario.Distribuir(faixas, util);
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

    /// <summary>
    /// O horário que está sendo escolhido para o registro manual, por cima das faixas.
    /// </summary>
    private void DesenharSelecao(DrawingContext contexto)
    {
        if (Selecao() is not (int inicio, int fim))
            return;

        var topo = (inicio - HoraInicial * 60) / 60.0 * AlturaDaHora;
        var altura = Math.Max(2, (fim - inicio) / 60.0 * AlturaDaHora);
        var area = new Rect(1, topo, Math.Max(0, ActualWidth - 3), altura);
        contexto.DrawRoundedRectangle(FundoDaSelecao, CanetaDaSelecao, area, 4, 4);

        var texto = new FormattedText(
            $"{HoraDoDia.Escrever(inicio)} – {(fim >= 24 * 60 ? "24:00" : HoraDoDia.Escrever(fim))}",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            Fonte,
            11,
            TintaDaSelecao,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);
        if (texto.Width + 10 > area.Width)
            return;

        // Num trecho curto o horário fica logo acima, para não sair da área escolhida.
        var y = altura >= texto.Height + 4 ? topo + 2 : Math.Max(0, topo - texto.Height - 1);
        contexto.DrawText(texto, new Point(area.Left + 5, y));
    }

    private sealed class MarcaDeAgora : Adorner
    {
        private readonly TrilhaDeHorario _painel;

        public MarcaDeAgora(TrilhaDeHorario painel)
            : base(painel)
        {
            _painel = painel;

            // A marca fica por cima da coluna só para ser vista: o clique tem de chegar à coluna.
            IsHitTestVisible = false;
        }

        protected override void OnRender(DrawingContext contexto)
        {
            if (_painel.MarcarAgora)
                _painel.DesenharAgora(contexto);
            _painel.DesenharSelecao(contexto);
        }
    }

    private static readonly Brush FundoDaSelecao = Congelar(new SolidColorBrush(Color.FromArgb(0x38, 0x1F, 0x3D, 0x32)));
    private static readonly Brush TintaDaSelecao = Congelar(new SolidColorBrush(Color.FromRgb(0x1F, 0x3D, 0x32)));
    private static readonly Pen CanetaDaSelecao = CriarCanetaDaSelecao();

    private static Brush Congelar(SolidColorBrush tinta)
    {
        tinta.Freeze();
        return tinta;
    }

    private static Pen CriarCanetaDaSelecao()
    {
        var pen = new Pen(new SolidColorBrush(Color.FromRgb(0x1F, 0x3D, 0x32)), 1.5)
        {
            DashStyle = new DashStyle([3, 2], 0),
        };
        pen.Freeze();
        return pen;
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
