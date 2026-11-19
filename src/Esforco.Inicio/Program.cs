using System.Diagnostics;

var raiz = Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
var soVerificar = args.Contains("--verificar");
var problema = Verificar(raiz);
if (problema is not null)
{
    Falhar(problema);
    return 1;
}

if (soVerificar)
{
    Console.WriteLine("Windows e .NET 10 estão prontos para abrir o Esforço.");
    return 0;
}

var dotnet = AcharDotnet()!;
var projeto = Path.Combine(raiz, "src", "Esforco.Ui", "Esforco.Ui.csproj");
var compilacao = Process.Start(new ProcessStartInfo
{
    FileName = dotnet,
    Arguments = $"build \"{projeto}\" -c Release --nologo",
    WorkingDirectory = raiz,
    UseShellExecute = false,
});
if (compilacao is null)
{
    Falhar("Não foi possível iniciar o comando dotnet para compilar o Esforço.");
    return 1;
}

compilacao.WaitForExit();
if (compilacao.ExitCode != 0)
{
    Falhar("A compilação do Esforço falhou. O texto acima mostra o erro. Confira se o SDK .NET 10 está instalado: https://dotnet.microsoft.com/download/dotnet/10.0");
    return compilacao.ExitCode;
}

var programa = Path.Combine(raiz, "src", "Esforco.Ui", "bin", "Release", "net10.0-windows", "Esforco.exe");
if (!File.Exists(programa))
{
    Falhar($"A compilação terminou, mas não encontrei o programa em {programa}.");
    return 1;
}

var abertura = new ProcessStartInfo
{
    FileName = "cmd.exe",
    Arguments = $"/c start \"\" \"{programa}\"",
    WorkingDirectory = raiz,
    UseShellExecute = false,
    CreateNoWindow = true,
};
abertura.Environment["ESFORCO_RAIZ"] = raiz;
if (Process.Start(abertura) is null)
{
    Falhar("O Esforço foi compilado, mas não abriu.");
    return 1;
}

return 0;

static string? Verificar(string raiz)
{
    if (!OperatingSystem.IsWindows())
        return "O Esforço só roda no Windows. O projeto src/Esforco.Ui é net10.0-windows, com WPF.";

    if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 14393))
    {
        return "Este Windows é antigo para o Esforço. O projeto src/Esforco.Ui é net10.0-windows e precisa do Windows 10, versão 1607 (compilação 14393) ou mais novo, em 64 bits."
            + Environment.NewLine + Environment.NewLine
            + "Atualize pelo Windows Update ou baixe o Windows 10 em https://www.microsoft.com/software-download/windows10";
    }

    var projeto = Path.Combine(raiz, "src", "Esforco.Ui", "Esforco.Ui.csproj");
    if (!File.Exists(projeto))
    {
        return "Não encontrei src/Esforco.Ui/Esforco.Ui.csproj ao lado deste executável. Deixe o Esforco.exe na raiz do repositório clonado.";
    }

    var dotnet = AcharDotnet();
    if (dotnet is null)
    {
        return "Não encontrei o .NET. O Esforco.exe da raiz abre o programa a partir do código, sem uma publicação prévia. O projeto src/Esforco.Ui pede o SDK .NET 10 para Windows x64, com WPF (net10.0-windows). O instalador do SDK já traz o runtime de aplicativos Windows."
            + Environment.NewLine + Environment.NewLine
            + "Baixe o SDK 10 em https://dotnet.microsoft.com/download/dotnet/10.0";
    }

    var sdks = Linhas(dotnet, "--list-sdks");
    var runtimes = Linhas(dotnet, "--list-runtimes");
    var temSdk10 = sdks.Any(linha => linha.StartsWith("10.", StringComparison.Ordinal));
    var temDesktop10 = runtimes.Any(linha => linha.StartsWith("Microsoft.WindowsDesktop.App 10.", StringComparison.Ordinal));

    if (!temSdk10)
    {
        var achados = sdks.Length == 0 ? "nenhum SDK" : string.Join(", ", sdks.Select(PrimeiroTrecho));
        return $"Encontrei o comando dotnet, mas não o SDK 10. O que está instalado: {achados}. O Esforço precisa do SDK 10 para compilar e abrir src/Esforco.Ui, que é net10.0-windows com WPF."
            + Environment.NewLine + Environment.NewLine
            + "Baixe o SDK 10 para Windows x64 em https://dotnet.microsoft.com/download/dotnet/10.0";
    }

    if (!temDesktop10)
    {
        return "O SDK 10 está instalado, mas falta o runtime Windows Desktop 10. Sem ele o WPF de src/Esforco.Ui não abre."
            + Environment.NewLine + Environment.NewLine
            + "Na mesma página, baixe o Windows Desktop Runtime 10: https://dotnet.microsoft.com/download/dotnet/10.0";
    }

    return null;
}

static string? AcharDotnet()
{
    var candidatos = new[]
    {
        Environment.GetEnvironmentVariable("DOTNET_ROOT"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "dotnet"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "dotnet"),
    };

    foreach (var pasta in candidatos)
    {
        if (string.IsNullOrWhiteSpace(pasta))
            continue;
        var exe = Path.Combine(pasta, "dotnet.exe");
        if (File.Exists(exe))
            return exe;
    }

    var caminho = Environment.GetEnvironmentVariable("PATH");
    if (caminho is null)
        return null;

    foreach (var pasta in caminho.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
    {
        var exe = Path.Combine(pasta.Trim(), "dotnet.exe");
        if (File.Exists(exe))
            return exe;
    }

    return null;
}

static string[] Linhas(string dotnet, string argumentos)
{
    var processo = Process.Start(new ProcessStartInfo
    {
        FileName = dotnet,
        Arguments = argumentos,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true,
    });
    if (processo is null)
        return [];

    var saida = processo.StandardOutput.ReadToEnd();
    processo.WaitForExit();
    return saida.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

static string PrimeiroTrecho(string linha)
{
    var espaco = linha.IndexOf(' ');
    return espaco < 0 ? linha : linha[..espaco];
}

static void Falhar(string texto)
{
    Console.Error.WriteLine(texto);
    Console.Error.WriteLine();
    Console.WriteLine("Pressione Enter para fechar.");
    Console.ReadLine();
}
