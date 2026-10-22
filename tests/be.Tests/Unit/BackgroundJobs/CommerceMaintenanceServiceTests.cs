using be.Infrastructure.BackgroundJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace be.Tests;

public sealed class CommerceMaintenanceServiceTests
{
    [Fact]
    public async Task ShutdownWhileWaitingForFirstTickCompletesWithoutFault()
    {
        using var services = new ServiceCollection().BuildServiceProvider();
        using var worker = new CommerceMaintenanceService(
            services.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<CommerceMaintenanceService>.Instance);

        await worker.StartAsync(CancellationToken.None);
        var execution = Assert.IsAssignableFrom<Task>(worker.ExecuteTask);
        Assert.False(execution.IsCompleted);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await worker.StopAsync(timeout.Token);
        await execution.WaitAsync(timeout.Token);

        Assert.True(execution.IsCompletedSuccessfully);
    }
}
