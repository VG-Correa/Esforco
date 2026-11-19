using System.Text.RegularExpressions;

namespace Esforco.Testes.Infra;

public class LeituraNaoMexeNaJanelaTestes
{
    private static readonly string[] Proibidos =
    [
        "SetForegroundWindow",
        "SetWindowPos",
        "ShowWindow",
        "MoveWindow",
        "CloseWindow",
        "DestroyWindow",
        "SetWindowPlacement",
        "SetWinEventHook",
        "keybd_event",
        "mouse_event",
        "SendInput",
        "SendMessage",
        "PostMessage",
        "SetActiveWindow",
        "BringWindowToTop",
        "SwitchToThisWindow",
        "SetParent",
        "EndTask",
        "AllowSetForegroundWindow",
        "LockSetForegroundWindow",
        "UseWindowsService",
        "AddWindowsService",
        "ServiceBase",
    ];

    [Fact]
    public void Codigo_nao_altera_janela_nem_instala_servico()
    {
        foreach (var arquivo in ArquivosDeCodigo())
        {
            var texto = File.ReadAllText(arquivo);
            foreach (var nome in Proibidos)
            {
                Assert.False(
                    Regex.IsMatch(texto, $@"\b{nome}\b"),
                    $"{Path.GetFileName(arquivo)} contém {nome}.");
            }
        }
    }

    [Fact]
    public void So_o_leitor_importa_a_api_do_windows()
    {
        var comImport = ArquivosDeCodigo()
            .Where(arquivo => File.ReadAllText(arquivo).Contains("DllImport", StringComparison.Ordinal))
            .ToList();

        var arquivo = Assert.Single(comImport);
        Assert.Equal("LeitorDeJanelaUser32.cs", Path.GetFileName(arquivo));
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
