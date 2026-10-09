using GatewayService.Api;
using GatewayService.Application;
using GatewayService.Infrastructure;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient<IBookingClients, BookingHttpClients>(client =>
    client.Timeout = TimeSpan.FromSeconds(10));
builder.Services.AddSingleton<InMemoryCircuitBreaker>();
builder.Services.AddSingleton<LoyaltyRetryQueue>();
builder.Services.AddHostedService<LoyaltyRetryWorker>();
builder.Services.AddScoped<BookingApplication>();

var app = builder.Build();
app.UseMiddleware<ExceptionMiddleware>();
app.MapGatewayEndpoints();
app.Run();
