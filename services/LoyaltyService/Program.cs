using LoyaltyService.Api;
using LoyaltyService.Application;
using LoyaltyService.Infrastructure;
var builder = WebApplication.CreateBuilder(args); builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter())); var cs = builder.Configuration.GetConnectionString("Database") ?? "Host=localhost;Port=5432;Database=loyalties;Username=program;Password=test"; builder.Services.AddSingleton<ILoyaltyRepository>(new PostgresLoyaltyRepository(cs)); builder.Services.AddScoped<LoyaltyApplication>(); var app = builder.Build(); await DatabaseInitializer.InitializeAsync(cs); app.MapLoyaltyEndpoints(); app.Run();
