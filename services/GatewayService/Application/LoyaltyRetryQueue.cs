using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace GatewayService.Application;

using GatewayService.Infrastructure;

public sealed record LoyaltyRetryTask(string Username);

/*public sealed class LoyaltyRetryQueue
{
    //private readonly Channel<LoyaltyRetryTask> _channel = Channel.CreateUnbounded<LoyaltyRetryTask>(
     //   new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
    
    private readonly List<LoyaltyRetryTask> _list = new();
    private readonly SemaphoreSlim _signal = new(0);

    //public ValueTask EnqueueAsync(LoyaltyRetryTask task, CancellationToken cancellationToken = default) =>
    //    _channel.Writer.WriteAsync(task, cancellationToken);
    
    public async ValueTask EnqueueAsync(LoyaltyRetryTask task, CancellationToken ct = default)
    {
        lock (_list) _list.Add(task);
        _signal.Release();
    }

    public IAsyncEnumerable<LoyaltyRetryTask> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}

public sealed class LoyaltyRetryWorker(
    LoyaltyRetryQueue queue,
    SemaphoreSlim signal,
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
                
                await signal.WaitAsync(stoppingToken);
                LoyaltyRetryTask task;
                lock (_list)
                {
                    if (_list.Count == 0) continue;
                    task = _list[0];
                    _list.RemoveAt(0);
                }
            }
        }
    }
}
*/

public sealed class LoyaltyRetryQueue
{
    private readonly List<LoyaltyRetryTask> _list = new();
    private readonly SemaphoreSlim _signal = new(0);
    private readonly object _gate = new();

    public ValueTask EnqueueAsync(LoyaltyRetryTask task, CancellationToken ct = default)
    {
        lock (_gate) _list.Add(task);
        _signal.Release();
        return ValueTask.CompletedTask;
    }

    public async IAsyncEnumerable<LoyaltyRetryTask> ReadAllAsync(
        [EnumeratorCancellation] CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await _signal.WaitAsync(ct).ConfigureAwait(false);

            LoyaltyRetryTask task;
            lock (_gate)
            {
                if (_list.Count == 0) continue;
                task = _list[0];
                _list.RemoveAt(0);
            }

            yield return task;
        }
    }
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
                catch (Exception exception) when (exception is DownstreamUnavailableException
                                                      or HttpRequestException
                                                      or TaskCanceledException)
                {
                    logger.LogWarning(exception,
                        "Could not decrease loyalty for {Username}; retrying in 10 seconds",
                        task.Username);
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
            }
        }
    }
}