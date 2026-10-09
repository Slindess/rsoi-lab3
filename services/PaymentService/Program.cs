using PaymentService.Api;
using PaymentService.Application;
using PaymentService.Infrastructure;
var builder = WebApplication.CreateBuilder(args); builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter())); var cs = builder.Configuration.GetConnectionString("Database") ?? "Host=localhost;Port=5432;Database=payments;Username=program;Password=test"; builder.Services.AddSingleton<IPaymentRepository>(new PostgresPaymentRepository(cs)); builder.Services.AddScoped<PaymentApplication>(); var app = builder.Build(); await DatabaseInitializer.InitializeAsync(cs); app.MapPaymentEndpoints(); app.Run();
