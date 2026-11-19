using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Esforco.Aplicacao;
using Esforco.Dominio;

namespace Esforco.Infra;

/// <summary>
/// Consulta a janela da frente. Nenhuma chamada muda foco, tamanho, posição ou fechamento.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class LeitorDeJanelaUser32 : ILeitorDeJanelaDaFrente
{
    private static readonly HashSet<string> Navegadores = new(StringComparer.OrdinalIgnoreCase)
    {
        "chrome",
        "msedge",
        "firefox",
        "brave",
        "opera",
    };

    private readonly ILeitorDeEnderecoDoNavegador _enderecos;
    private readonly ILeitorDeConversaDoNavegador _conversas;
    private readonly Dictionary<int, (string Amigavel, string Processo)> _programas = new();

    public LeitorDeJanelaUser32(ILeitorDeEnderecoDoNavegador enderecos, ILeitorDeConversaDoNavegador conversas)
    {
        _enderecos = enderecos;
        _conversas = conversas;
    }

    public AmostraDeJanela? Ler()
    {
        var janela = Native.GetForegroundWindow();
        if (janela == IntPtr.Zero)
            return null;

        _ = Native.GetWindowThreadProcessId(janela, out var pid);
        var (amigavel, processo) = Nomes((int)pid);
        var titulo = Titulo(janela);
        if (titulo.Length > 2000)
            titulo = titulo[..2000];

        var endereco = "";
        if (Navegadores.Contains(processo))
        {
            try
            {
                endereco = _enderecos.Ler(janela).Trim();
            }
            catch (Exception)
            {
                endereco = "";
            }
        }

        if (endereco.Length > 2000)
            endereco = endereco[..2000];

        var conversa = "";
        if (Navegadores.Contains(processo) && InterpretadorDeChatwoot.EhAba(titulo, endereco))
        {
            try
            {
                conversa = _conversas.LerTitulo(janela).Trim();
            }
            catch (Exception)
            {
                conversa = "";
            }
        }

        if (conversa.Length > 200)
            conversa = conversa[..200];

        return new AmostraDeJanela(
            amigavel,
            titulo,
            Native.IsIconic(janela),
            endereco,
            conversa,
            (int)pid == Environment.ProcessId);
    }

    public AmostraDeJanela? LerReuniaoMinimizada()
    {
        var frente = Native.GetForegroundWindow();
        AmostraDeJanela? encontrada = null;
        Native.EnumWindows(Visitar, IntPtr.Zero);
        return encontrada;

        bool Visitar(IntPtr janela, IntPtr parametro)
        {
            if (janela == frente || !Native.IsWindowVisible(janela) || !Native.IsIconic(janela))
                return true;

            var titulo = Titulo(janela);
            if (titulo.Length == 0 || InterpretadorDeReuniao.Ler(titulo) is null)
                return true;

            _ = Native.GetWindowThreadProcessId(janela, out uint pid);
            var (amigavel, _) = Nomes((int)pid);
            encontrada = new AmostraDeJanela(amigavel, titulo, true);
            return false;
        }
    }

    private (string Amigavel, string Processo) Nomes(int pid)
    {
        if (pid <= 0)
            return ("Programa desconhecido", "");

        lock (_programas)
        {
            if (_programas.TryGetValue(pid, out var guardado))
                return guardado;
        }

        var nomes = LerNome(pid);
        lock (_programas)
        {
            if (_programas.Count > 128)
                _programas.Clear();
            _programas[pid] = nomes;
        }

        return nomes;
    }

    private static (string Amigavel, string Processo) LerNome(int pid)
    {
        try
        {
            using var processo = Process.GetProcessById(pid);
            try
            {
                var descricao = processo.MainModule?.FileVersionInfo.FileDescription;
                if (!string.IsNullOrWhiteSpace(descricao))
                    return (Cortar(descricao.Trim()), processo.ProcessName);
            }
            catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException or UnauthorizedAccessException)
            {
            }

            return (Cortar(processo.ProcessName), processo.ProcessName);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException or UnauthorizedAccessException)
        {
            return ("Programa desconhecido", "");
        }
    }

    private static string Titulo(IntPtr janela)
    {
        var tamanho = Native.GetWindowTextLength(janela);
        if (tamanho <= 0)
            return "";

        var texto = new StringBuilder(tamanho + 1);
        _ = Native.GetWindowText(janela, texto, texto.Capacity);
        return texto.ToString().Trim();
    }

    private static string Cortar(string nome) => nome.Length > 200 ? nome[..200] : nome;

    [SupportedOSPlatform("windows")]
    private static class Native
    {
        [DllImport("user32.dll")]
        internal static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsIconic(IntPtr hWnd);

        internal delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsWindowVisible(IntPtr hWnd);
    }
}

/// <summary>
/// Pede ao Windows que avise a janela do Esforço quando a combinação de teclas for apertada.
/// É só o registro de atalho do Windows: não há gancho de teclado e nenhuma tecla é lida ou enviada.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class AtalhoGlobalUser32 : IAtalhoGlobal
{
    private const uint Alt = 0x0001;
    private const uint Ctrl = 0x0002;
    private const uint Shift = 0x0004;
    private const uint Win = 0x0008;
    private const uint SemRepetir = 0x4000;

    public int Mensagem => 0x0312;

    public bool Registrar(IntPtr janela, int id, AtalhoDeTeclado atalho)
    {
        ArgumentNullException.ThrowIfNull(atalho);
        if (janela == IntPtr.Zero)
            return false;

        var modificadores = SemRepetir;
        if (atalho.Alt)
            modificadores |= Alt;
        if (atalho.Ctrl)
            modificadores |= Ctrl;
        if (atalho.Shift)
            modificadores |= Shift;
        if (atalho.Win)
            modificadores |= Win;

        return Native.RegisterHotKey(janela, id, modificadores, (uint)atalho.Codigo);
    }

    public void Liberar(IntPtr janela, int id)
    {
        if (janela != IntPtr.Zero)
            _ = Native.UnregisterHotKey(janela, id);
    }

    private static class Native
    {
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}
