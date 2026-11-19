using System.Text.RegularExpressions;

namespace Esforco.Testes.Infra;

public class AtalhoSoUsaRegistroDoWindowsTestes
{
    private static readonly string[] Permitidas =
    [
        "EnumWindows",
        "GetForegroundWindow",
        "GetWindowText",
        "GetWindowTextLength",
        "GetWindowThreadProcessId",
        "IsIconic",
        "IsWindowVisible",
        "RegisterHotKey",
        "UnregisterHotKey",
    ];

    private static readonly string[] LeituraDeTeclado =
    [
        "SetWindowsHookEx",
        "SetWindowsHookExW",
        "SetWindowsHookExA",
        "UnhookWindowsHookEx",
        "CallNextHookEx",
        "GetAsyncKeyState",
        "GetKeyState",
        "GetKeyboardState",
        "RegisterRawInputDevices",
        "GetRawInputData",
        "WH_KEYBOARD",
        "WH_KEYBOARD_LL",
    ];

    [Fact]
    public void As_unicas_funcoes_do_windows_importadas_sao_as_de_leitura_e_as_do_registro_de_atalho()
    {
        var importadas = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var arquivo in ArquivosDeCodigo())
        {
            foreach (Match achado in Regex.Matches(File.ReadAllText(arquivo), @"\bextern\s+[\w.<>\[\]?]+\s+(\w+)\s*\("))
                importadas.Add(achado.Groups[1].Value);
        }

        Assert.Equal(Permitidas, importadas);
    }

    [Fact]
    public void Codigo_nao_instala_gancho_nem_le_o_teclado()
    {
        foreach (var arquivo in ArquivosDeCodigo())
        {
            var texto = File.ReadAllText(arquivo);
            foreach (var nome in LeituraDeTeclado)
            {
                Assert.False(
                    Regex.IsMatch(texto, $@"\b{nome}\b"),
                    $"{Path.GetFileName(arquivo)} contém {nome}.");
            }
        }
    }

    private static IReadOnlyList<string> ArquivosDeCodigo()
    {
        var pasta = new DirectoryInfo(AppContext.BaseDirectory);
        while (pasta is not null && !File.Exists(Path.Combine(pasta.FullName, "Esforco.slnx")))
            pasta = pasta.Parent;

        Assert.NotNull(pasta);
        return Directory.GetFiles(Path.Combine(pasta.FullName, "src"), "*.cs", SearchOption.AllDirectories);
    }
}
