using Esforco.Aplicacao;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Esforco.Ui;

public sealed class PulsoDeLeitura : BackgroundService
{
    private readonly ServicoDeEsforco _servico;
    private readonly ILogger<PulsoDeLeitura> _log;

    public PulsoDeLeitura(ServicoDeEsforco servico, ILogger<PulsoDeLeitura> log)
    {
        _servico = servico;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            try
            {
                _servico.Pulso();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _log.LogError(ex, "Falha ao registrar a janela da frente.");
            }
        }
    }
}
