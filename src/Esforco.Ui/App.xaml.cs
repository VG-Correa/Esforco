using System.Windows;
using Esforco.Aplicacao;
using Esforco.Infra;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Win32;

namespace Esforco.Ui;

public partial class App : Application
{
    private IHost? _host;
    private ServicoDeEsforco? _servico;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
            {
                Args = e.Args,
                ContentRootPath = PastaDoPrograma.Obter(),
            });

            var lifetime = builder.Services.FirstOrDefault(d => d.ServiceType == typeof(IHostLifetime));
            if (lifetime is not null)
                builder.Services.Remove(lifetime);

            builder.Services.AddSingleton<IHostLifetime, VidaDaJanela>();
            builder.Services.AddSingleton<IRelogio, RelogioDoSistema>();
            builder.Services.AddSingleton<ILeitorDeEnderecoDoNavegador, LeitorDeEnderecoDaBarra>();
            builder.Services.AddSingleton<ILeitorDeConversaDoNavegador, LeitorDeConversaDoChatwoot>();
            builder.Services.AddSingleton<ILeitorDeJanelaDaFrente, LeitorDeJanelaUser32>();
            builder.Services.AddSingleton<IFonteDeRegrasDeEscritorio>(_ =>
                new FonteDeRegrasDeEscritorioJson(FonteDeRegrasDeEscritorioJson.CaminhoAoLadoDoExecutavel()));
            builder.Services.AddSingleton<IRepositorioDeIntervalos>(_ =>
                new RepositorioSqliteDeIntervalos(CaminhoDoBanco()));
            builder.Services.AddSingleton<IRepositorioDeVinculos>(_ =>
                new RepositorioSqliteDeVinculos(CaminhoDoBanco()));
            builder.Services.AddSingleton<ServicoDeVinculos>();
            builder.Services.AddSingleton<IAtalhoGlobal, AtalhoGlobalUser32>();
            builder.Services.AddSingleton<AtalhoDaJanela>();
            builder.Services.AddSingleton<ServicoDeEsforco>();
            builder.Services.AddHostedService<PulsoDeLeitura>();
            builder.Services.AddSingleton<ModeloDaJanela>();
            builder.Services.AddSingleton<MainWindow>();

            _host = builder.Build();
            await _host.StartAsync();
            _servico = _host.Services.GetRequiredService<ServicoDeEsforco>();
            SystemEvents.SessionSwitch += AoTrocarSessao;
            SystemEvents.PowerModeChanged += AoMudarEnergia;

            var janela = _host.Services.GetRequiredService<MainWindow>();
            MainWindow = janela;
            janela.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Não foi possível abrir o Esforço." + Environment.NewLine + Environment.NewLine + ex.Message,
                "Esforço",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            await EncerrarHost();
            Shutdown(1);
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        SystemEvents.SessionSwitch -= AoTrocarSessao;
        SystemEvents.PowerModeChanged -= AoMudarEnergia;

        try
        {
            _servico?.Parar();
        }
        catch (Exception)
        {
        }

        await EncerrarHost();
        base.OnExit(e);
    }

    private static string CaminhoDoBanco() =>
        BancoDoEsforco.Resolver(
            PastaDoPrograma.Obter(),
            Environment.CurrentDirectory,
            Environment.GetEnvironmentVariable("ESFORCO_RAIZ"));

    private async Task EncerrarHost()
    {
        if (_host is null)
            return;

        var host = _host;
        _host = null;
        await host.StopAsync(TimeSpan.FromSeconds(3));
        host.Dispose();
    }

    private void AoTrocarSessao(object sender, SessionSwitchEventArgs e)
    {
        if (e.Reason == SessionSwitchReason.SessionLock)
            _servico?.Ausentar();
        else if (e.Reason == SessionSwitchReason.SessionUnlock)
            _servico?.Retomar();
    }

    private void AoMudarEnergia(object sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Suspend)
            _servico?.Ausentar();
        else if (e.Mode == PowerModes.Resume)
            _servico?.Retomar();
    }
}
