using Microsoft.Extensions.Hosting;

namespace Esforco.Ui;

internal sealed class VidaDaJanela : IHostLifetime
{
    public Task WaitForStartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
