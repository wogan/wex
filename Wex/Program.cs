using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Wex;
using Wex.Api;
using Wex.Service;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddValidatorsFromAssemblyContaining<CreateTransactionRequestValidator>();

builder.Services
    .AddHttpClient<ExchangeRatesService>()
    .AddPolicyHandler(HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))))
    .AddPolicyHandler(HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromSeconds(30)));

builder.Services.AddDbContext<Database>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Database")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/cards", CardsApi.CreateCard)
    .WithName("CreateCard");

app.MapGet("/cards/{id:int}", CardsApi.GetCard)
    .WithName("GetCard");

app.MapGet("/cards/{id:int}/balance", (
        int id,
        string currency,
        string country,
        ExchangeRatesService exchangeRatesService,
        Database database,
        CancellationToken cancellationToken) =>
    CardsApi.GetCardBalanceInCurrency(
        new CardBalanceRequest(id, currency, country),
        exchangeRatesService,
        database,
        cancellationToken))
    .WithName("GetCardBalanceInCurrency");

app.MapPost("/transactions", TransactionsApi.CreateTransaction)
    .WithName("CreateTransaction");

app.MapGet("/transactions/{id:int}", (
        int id,
        string? currency,
        string? country,
        Database database,
        ExchangeRatesService exchangeRatesService,
        CancellationToken cancellationToken) =>
    TransactionsApi.GetTransaction(
        id,
        currency,
        country,
        database,
        exchangeRatesService,
        cancellationToken))
    .WithName("GetTransaction");

app.Run();
public partial class Program { }
