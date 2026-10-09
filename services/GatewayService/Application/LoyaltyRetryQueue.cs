using System.Threading.Channels;

namespace GatewayService.Application;

using GatewayService.Infrastructure;

public sealed record LoyaltyRetryTask(string Username);

public sealed class LoyaltyRetryQueue
{
    private readonly Channel<LoyaltyRetryTask> _channel = Channel.CreateUnbounded<LoyaltyRetryTask>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

    public ValueTask EnqueueAsync(LoyaltyRetryTask task, CancellationToken cancellationToken = default) =>
        _channel.Writer.WriteAsync(task, cancellationToken);

    public IAsyncEnumerable<LoyaltyRetryTask> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}

public sealed class LoyaltyRetryWorker(
    LoyaltyRetryQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<LoyaltyRetryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var task in queue.ReadAllAsync(stoppingToken))
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var clients = scope.ServiceProvider.GetRequiredService<IBookingClients>();
                    await clients.ChangeLoyaltyAsync(task.Username, false, stoppingToken);
                    break;
                }
                catch (Exception exception) when (exception is DownstreamUnavailableException or HttpRequestException or TaskCanceledException)
                {
                    logger.LogWarning(exception,
                        "Could not decrease loyalty for {Username}; retrying in 10 seconds", task.Username);
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
            }
        }
    }
}
