using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mochi.Application.Abstractions;
using Mochi.Application.Rollups;

namespace Mochi.Infrastructure.Rollups;

/// <summary>
/// Runs the rollup job daily at 00:05 UTC for the just-closed day (ADR 0003).
/// Failures are logged and retried at the next tick; use the admin rollup
/// endpoint to rerun a specific day.
/// </summary>
public sealed class DailyRollupHostedService(IServiceScopeFactory scopes, IClock clock, ILogger<DailyRollupHostedService> log) : BackgroundService
{
    private static readonly TimeSpan RunAt = TimeSpan.FromMinutes(5);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(DelayUntilNextRun(clock.UtcNow), stoppingToken);

            var closedDay = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime).AddDays(-1);
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<RollupJob>().RunForDayAsync(closedDay, stoppingToken);
                log.LogInformation("rollup complete for {Day}", closedDay);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "rollup failed for {Day}", closedDay);
            }
        }
    }

    private static TimeSpan DelayUntilNextRun(DateTimeOffset now)
    {
        var next = now.UtcDateTime.Date.Add(RunAt);
        if (next <= now.UtcDateTime) next = next.AddDays(1);
        return next - now.UtcDateTime;
    }
}
