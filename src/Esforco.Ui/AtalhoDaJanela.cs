using System.Windows.Interop;
using Esforco.Aplicacao;
using Esforco.Dominio;

namespace Esforco.Ui;

/// <summary>
/// Liga o atalho de vínculo à janela do Esforço. Usa só o registro de atalho do Windows:
/// o Windows avisa a janela quando a combinação é apertada, e nenhuma outra tecla chega aqui.
/// O atalho vale enquanto o Esforço estiver aberto.
/// </summary>
public sealed class AtalhoDaJanela
{
    private const int Numero = 0x4553;

    private readonly IAtalhoGlobal _global;
    private readonly ServicoDeVinculos _vinculos;
    private IntPtr _janela;
    private Action? _aoApertar;
    private bool _ouvindo;

    public AtalhoDaJanela(IAtalhoGlobal global, ServicoDeVinculos vinculos)
    {
        _global = global;
        _vinculos = vinculos;
        Atual = AtalhoDeTeclado.Padrao;
    }

    public event Action? Mudou;

    public AtalhoDeTeclado Atual { get; private set; }

    public bool Registrado { get; private set; }

    public void Ligar(IntPtr janela, Action aoApertar)
    {
        _janela = janela;
        _aoApertar = aoApertar;
        Atual = _vinculos.LerAtalho();
        Registrado = _global.Registrar(janela, Numero, Atual);
        if (!_ouvindo)
        {
            ComponentDispatcher.ThreadPreprocessMessage += AoReceber;
            _ouvindo = true;
        }

        Mudou?.Invoke();
    }

    /// <summary>
    /// Troca a combinação. Se o Windows recusar a nova, a anterior volta a valer e o retorno explica.
    /// </summary>
    public string? Trocar(AtalhoDeTeclado novo)
    {
        ArgumentNullException.ThrowIfNull(novo);
        if (_janela == IntPtr.Zero)
            return "A janela do Esforço ainda não abriu.";

        if (novo == Atual && Registrado)
            return null;

        var anterior = Atual;
        var estava = Registrado;
        if (estava)
            _global.Liberar(_janela, Numero);

        if (_global.Registrar(_janela, Numero, novo))
        {
            Atual = novo;
            Registrado = true;
            _vinculos.DefinirAtalho(novo);
            Mudou?.Invoke();
            return null;
        }

        Registrado = estava && _global.Registrar(_janela, Numero, anterior);
        Mudou?.Invoke();
        return $"O Windows recusou {novo}: outro programa já usa essa combinação. Escolha outra.";
    }

    public void Desligar()
    {
        if (_ouvindo)
        {
            ComponentDispatcher.ThreadPreprocessMessage -= AoReceber;
            _ouvindo = false;
        }

        if (Registrado)
            _global.Liberar(_janela, Numero);

        Registrado = false;
        _janela = IntPtr.Zero;
        _aoApertar = null;
    }

    private void AoReceber(ref MSG mensagem, ref bool tratada)
    {
        if (mensagem.message != _global.Mensagem || mensagem.hwnd != _janela || (long)mensagem.wParam != Numero)
            return;

        tratada = true;
        _aoApertar?.Invoke();
    }
}
