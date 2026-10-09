using GatewayService.Infrastructure;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace GatewayService.Tests;

public sealed class CircuitBreakerTests
{
    [Fact]
    public async Task OpensAfterConfiguredNumberOfFailuresAndShortCircuitsNextCall()
    {
        var breaker = CreateBreaker(threshold: 2);
        var calls = 0;
        Task<int> FailingRequest()
        {
            calls++;
            return Task.FromException<int>(new HttpRequestException("offline"));
        }

        await Assert.ThrowsAsync<DownstreamUnavailableException>(() => breaker.ExecuteAsync("Loyalty Service", FailingRequest));
        await Assert.ThrowsAsync<DownstreamUnavailableException>(() => breaker.ExecuteAsync("Loyalty Service", FailingRequest));
        await Assert.ThrowsAsync<DownstreamUnavailableException>(() => breaker.ExecuteAsync("Loyalty Service", FailingRequest));

        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task SuccessfulRequestResetsFailureCounter()
    {
        var breaker = CreateBreaker(threshold: 2);
        await Assert.ThrowsAsync<DownstreamUnavailableException>(() => breaker.ExecuteAsync<int>("Payment Service", () => Task.FromException<int>(new HttpRequestException())));
        Assert.Equal(42, await breaker.ExecuteAsync("Payment Service", () => Task.FromResult(42)));
        await Assert.ThrowsAsync<DownstreamUnavailableException>(() => breaker.ExecuteAsync<int>("Payment Service", () => Task.FromException<int>(new HttpRequestException())));

        Assert.Equal(42, await breaker.ExecuteAsync("Payment Service", () => Task.FromResult(42)));
    }

    [Fact]
    public async Task KeepsIndependentStateForEveryService()
    {
        var breaker = CreateBreaker(threshold: 1);
        await Assert.ThrowsAsync<DownstreamUnavailableException>(() => breaker.ExecuteAsync<int>("Loyalty Service", () => Task.FromException<int>(new HttpRequestException())));

        Assert.Equal(7, await breaker.ExecuteAsync("Reservation Service", () => Task.FromResult(7)));
    }

    private static InMemoryCircuitBreaker CreateBreaker(int threshold)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["CircuitBreaker:FailureThreshold"] = threshold.ToString(),
            ["CircuitBreaker:BreakDurationSeconds"] = "60"
        }).Build();
        return new InMemoryCircuitBreaker(configuration);
    }
}
