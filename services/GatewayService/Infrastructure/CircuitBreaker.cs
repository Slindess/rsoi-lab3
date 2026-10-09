using System.Collections.Concurrent;

namespace GatewayService.Infrastructure;

public sealed class DownstreamUnavailableException(string serviceName, Exception? innerException = null)
    : Exception($"{serviceName} unavailable", innerException)
{
    public string ServiceName { get; } = serviceName;
}

public sealed class CircuitBreakerOptions
{
    public int FailureThreshold { get; init; } = 3;
    public int BreakDurationSeconds { get; init; } = 10;
}

public sealed class InMemoryCircuitBreaker(IConfiguration configuration)
{
    private sealed class CircuitState
    {
        public readonly object Sync = new();
        public int ConsecutiveFailures;
        public DateTimeOffset? OpenedAt;
        public bool ProbeInProgress;
    }

    private readonly ConcurrentDictionary<string, CircuitState> _circuits = new();
    private readonly CircuitBreakerOptions _options = configuration
        .GetSection("CircuitBreaker")
        .Get<CircuitBreakerOptions>() ?? new CircuitBreakerOptions();

    public async Task<T> ExecuteAsync<T>(string serviceName, Func<Task<T>> operation)
    {
        var state = _circuits.GetOrAdd(serviceName, _ => new CircuitState());
        var isProbe = false;

        lock (state.Sync)
        {
            if (state.OpenedAt is { } openedAt)
            {
                if (DateTimeOffset.UtcNow - openedAt < TimeSpan.FromSeconds(_options.BreakDurationSeconds) ||
                    state.ProbeInProgress)
                    throw new DownstreamUnavailableException(serviceName);

                state.ProbeInProgress = true;
                isProbe = true;
            }
        }

        try
        {
            var result = await operation();
            lock (state.Sync)
            {
                state.ConsecutiveFailures = 0;
                state.OpenedAt = null;
                state.ProbeInProgress = false;
            }
            return result;
        }
        catch (Exception exception) when (IsAvailabilityFailure(exception))
        {
            lock (state.Sync)
            {
                state.ProbeInProgress = false;
                state.ConsecutiveFailures++;
                if (isProbe || state.ConsecutiveFailures >= Math.Max(1, _options.FailureThreshold))
                    state.OpenedAt = DateTimeOffset.UtcNow;
            }
            throw new DownstreamUnavailableException(serviceName, exception);
        }
    }

    private static bool IsAvailabilityFailure(Exception exception) => exception switch
    {
        HttpRequestException { StatusCode: null } => true,
        HttpRequestException { StatusCode: >= System.Net.HttpStatusCode.InternalServerError } => true,
        TaskCanceledException => true,
        _ => false
    };
}
