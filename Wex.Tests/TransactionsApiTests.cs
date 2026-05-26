using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Wex.Api;
using Wex.Model;
using Wex.Service;

namespace Wex.Tests;

public class TransactionsApiTests
{
    private static IConfiguration CreateEmptyConfiguration() =>
        new ConfigurationBuilder().Build();
    [Fact]
    public async Task CreateTransaction_WhenRequestIsValidAndCardExists_CreatesTransaction()
    {
        await using var database = CreateDatabase();

        var card = new Card
        {
            LimitAmount = 1000
        };

        database.Cards.Add(card);
        await database.SaveChangesAsync();

        var validator = new CreateTransactionRequestValidator();
        var transactionDate = new DateTimeOffset(2025, 1, 15, 0, 0, 0, TimeSpan.Zero);

        var result = await TransactionsApi.CreateTransaction(
            new CreateTransactionRequest(card.Id, 125.50m, transactionDate, "Coffee"),
            validator,
            database,
            CancellationToken.None);

        var created = result.Should().BeOfType<Created<TransactionResponse>>().Subject;

        created.Location.Should().StartWith("/transactions/");
        created.Value.Should().NotBeNull();
        created.Value!.Id.Should().BeGreaterThan(0);
        created.Value.CardId.Should().Be(card.Id);
        created.Value.Amount.Should().Be(125.50m);
        created.Value.Date.Should().Be(transactionDate);
        created.Value.Description.Should().Be("Coffee");

        var transaction = await database.Transactions.SingleAsync();

        transaction.Id.Should().Be(created.Value.Id);
        transaction.CardId.Should().Be(card.Id);
        transaction.Amount.Should().Be(125.50m);
        transaction.Date.Should().Be(transactionDate);
        transaction.Description.Should().Be("Coffee");
    }

    [Fact]
    public async Task CreateTransaction_WhenRequestIsInvalid_ReturnsValidationProblem()
    {
        await using var database = CreateDatabase();

        var validator = new CreateTransactionRequestValidator();

        var result = await TransactionsApi.CreateTransaction(
            new CreateTransactionRequest(0, 0, DateTimeOffset.UtcNow.AddDays(1), ""),
            validator,
            database,
            CancellationToken.None);

        var validationProblem = result.Should().BeOfType<ProblemHttpResult>().Subject;

        validationProblem.ProblemDetails.Status.Should().Be(StatusCodes.Status400BadRequest);
        validationProblem.ProblemDetails.Should().BeOfType<HttpValidationProblemDetails>()
            .Which.Errors.Should().ContainKeys(
                nameof(CreateTransactionRequest.CardId),
                nameof(CreateTransactionRequest.Amount),
                nameof(CreateTransactionRequest.Description),
                nameof(CreateTransactionRequest.Date));

        database.Transactions.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateTransaction_WhenCardDoesNotExist_ReturnsNotFound()
    {
        await using var database = CreateDatabase();

        var validator = new CreateTransactionRequestValidator();

        var result = await TransactionsApi.CreateTransaction(
            new CreateTransactionRequest(
                123,
                25,
                new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
                "Unknown card purchase"),
            validator,
            database,
            CancellationToken.None);

        var notFound = result.Should().BeOfType<NotFound<string>>().Subject;

        notFound.Value.Should().Be("Card with id 123 was not found.");
        database.Transactions.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTransaction_WhenTransactionExistsWithoutCurrencyAndCountry_ReturnsTransaction()
    {
        await using var database = CreateDatabase();

        var card = new Card
        {
            LimitAmount = 1000
        };

        database.Cards.Add(card);
        await database.SaveChangesAsync();

        var transaction = new Transaction
        {
            CardId = card.Id,
            Amount = 75.25m,
            Date = new DateTimeOffset(2025, 2, 10, 0, 0, 0, TimeSpan.Zero),
            Description = "Groceries"
        };

        database.Transactions.Add(transaction);
        await database.SaveChangesAsync();

        using var httpClient = CreateHttpClient("""
        {
          "data": []
        }
        """);

        var exchangeRatesService = new ExchangeRatesService(httpClient, CreateEmptyConfiguration());

        var result = await TransactionsApi.GetTransaction(
            transaction.Id,
            currency: null,
            country: null,
            database,
            exchangeRatesService,
            CancellationToken.None);

        var ok = result.Should().BeOfType<Ok<TransactionResponse>>().Subject;

        ok.Value.Should().BeEquivalentTo(new TransactionResponse(
            transaction.Id,
            card.Id,
            75.25m,
            transaction.Date,
            "Groceries"));
    }

    [Fact]
    public async Task GetTransaction_WhenTransactionDoesNotExist_ReturnsNotFound()
    {
        await using var database = CreateDatabase();

        using var httpClient = CreateHttpClient("""
        {
          "data": []
        }
        """);

        var exchangeRatesService = new ExchangeRatesService(httpClient, CreateEmptyConfiguration());

        var result = await TransactionsApi.GetTransaction(
            123,
            currency: null,
            country: null,
            database,
            exchangeRatesService,
            CancellationToken.None);

        var notFound = result.Should().BeOfType<NotFound<string>>().Subject;

        notFound.Value.Should().Be("Transaction with id 123 was not found.");
    }

    [Fact]
    public async Task GetTransaction_WhenCurrencyOrCountryIsMissing_ReturnsUnconvertedTransaction()
    {
        await using var database = CreateDatabase();

        var card = new Card
        {
            LimitAmount = 1000
        };

        database.Cards.Add(card);
        await database.SaveChangesAsync();

        var transaction = new Transaction
        {
            CardId = card.Id,
            Amount = 40,
            Date = new DateTimeOffset(2025, 3, 1, 0, 0, 0, TimeSpan.Zero),
            Description = "Lunch"
        };

        database.Transactions.Add(transaction);
        await database.SaveChangesAsync();

        using var httpClient = CreateHttpClient("""
        {
          "data": []
        }
        """);

        var exchangeRatesService = new ExchangeRatesService(httpClient, CreateEmptyConfiguration());

        var result = await TransactionsApi.GetTransaction(
            transaction.Id,
            currency: "Dollar",
            country: null,
            database,
            exchangeRatesService,
            CancellationToken.None);

        var ok = result.Should().BeOfType<Ok<TransactionResponse>>().Subject;

        ok.Value.Should().BeEquivalentTo(new TransactionResponse(
            transaction.Id,
            card.Id,
            40,
            transaction.Date,
            "Lunch"));
    }

    [Fact]
    public async Task GetTransaction_WhenExchangeRateIsAvailable_ReturnsConvertedTransaction()
    {
        await using var database = CreateDatabase();

        var card = new Card
        {
            LimitAmount = 1000
        };

        database.Cards.Add(card);
        await database.SaveChangesAsync();

        var transaction = new Transaction
        {
            CardId = card.Id,
            Amount = 100,
            Date = new DateTimeOffset(2025, 4, 15, 0, 0, 0, TimeSpan.Zero),
            Description = "Hotel"
        };

        database.Transactions.Add(transaction);
        await database.SaveChangesAsync();

        using var httpClient = CreateHttpClient("""
        {
          "data": [
            {
              "record_date": "2025-04-01",
              "country": "Canada",
              "currency": "Dollar",
              "country_currency_desc": "Canada-Dollar",
              "exchange_rate": "1.35"
            }
          ]
        }
        """);

        var exchangeRatesService = new ExchangeRatesService(httpClient, CreateEmptyConfiguration());

        var result = await TransactionsApi.GetTransaction(
            transaction.Id,
            currency: "Dollar",
            country: "Canada",
            database,
            exchangeRatesService,
            CancellationToken.None);

        var ok = result.Should().BeOfType<Ok<ConvertedTransactionResponse>>().Subject;

        ok.Value.Should().BeEquivalentTo(new ConvertedTransactionResponse(
            transaction.Id,
            "Hotel",
            transaction.Date,
            100,
            1.35m,
            135,
            "Dollar"));
    }

    [Fact]
    public async Task GetTransaction_WhenExchangeRateIsUnavailable_ReturnsBadRequest()
    {
        await using var database = CreateDatabase();

        var card = new Card
        {
            LimitAmount = 1000
        };

        database.Cards.Add(card);
        await database.SaveChangesAsync();

        var transaction = new Transaction
        {
            CardId = card.Id,
            Amount = 100,
            Date = new DateTimeOffset(2025, 4, 15, 0, 0, 0, TimeSpan.Zero),
            Description = "Hotel"
        };

        database.Transactions.Add(transaction);
        await database.SaveChangesAsync();

        using var httpClient = CreateHttpClient("""
        {
          "data": []
        }
        """);

        var exchangeRatesService = new ExchangeRatesService(httpClient, CreateEmptyConfiguration());

        var result = await TransactionsApi.GetTransaction(
            transaction.Id,
            currency: "Dollar",
            country: "Canada",
            database,
            exchangeRatesService,
            CancellationToken.None);

        var badRequest = result.Should().BeOfType<BadRequest<string>>().Subject;

        badRequest.Value.Should().Be(
            $"Transaction {transaction.Id} cannot be converted to Dollar because no exchange rate is available within 6 months on or before the transaction date.");
    }

    private static Database CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<Database>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new Database(options);
    }

    private static HttpClient CreateHttpClient(string responseBody)
    {
        return new HttpClient(new StubHttpMessageHandler(responseBody));
    }

    private sealed class StubHttpMessageHandler(string responseBody) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody)
            };

            return Task.FromResult(response);
        }
    }
}