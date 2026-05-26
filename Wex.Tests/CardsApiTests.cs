using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Wex.Api;
using Wex.Model;
using Wex.Service;

namespace Wex.Tests;

public class CardsApiTests
{
    private static IConfiguration CreateEmptyConfiguration() =>
        new ConfigurationBuilder().Build();
    [Fact]
    public async Task CreateCard_WhenLimitAmountIsPositive_CreatesCard()
    {
        await using var database = CreateDatabase();

        var result = await CardsApi.CreateCard(
            new CreateCardRequest(5000),
            database,
            CancellationToken.None);

        var created = result.Should().BeOfType<Created<CardResponse>>().Subject;

        created.Location.Should().StartWith("/cards/");
        created.Value.Should().NotBeNull();
        created.Value!.Id.Should().BeGreaterThan(0);
        created.Value.LimitAmount.Should().Be(5000);

        var card = await database.Cards.SingleAsync();

        card.Id.Should().Be(created.Value.Id);
        card.LimitAmount.Should().Be(5000);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CreateCard_WhenLimitAmountIsNotPositive_ReturnsBadRequest(long limitAmount)
    {
        await using var database = CreateDatabase();

        var result = await CardsApi.CreateCard(
            new CreateCardRequest(limitAmount),
            database,
            CancellationToken.None);

        var badRequest = result.Should().BeOfType<BadRequest<string>>().Subject;

        badRequest.Value.Should().Be("LimitAmount must be greater than zero.");
        database.Cards.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCard_WhenCardExists_ReturnsCard()
    {
        await using var database = CreateDatabase();

        var card = new Card
        {
            LimitAmount = 2500
        };

        database.Cards.Add(card);
        await database.SaveChangesAsync();

        var result = await CardsApi.GetCard(
            card.Id,
            database,
            CancellationToken.None);

        var ok = result.Should().BeOfType<Ok<CardResponse>>().Subject;

        ok.Value.Should().BeEquivalentTo(new CardResponse(card.Id, 2500));
    }

    [Fact]
    public async Task GetCard_WhenCardDoesNotExist_ReturnsNotFound()
    {
        await using var database = CreateDatabase();

        var result = await CardsApi.GetCard(
            123,
            database,
            CancellationToken.None);

        var notFound = result.Should().BeOfType<NotFound<string>>().Subject;

        notFound.Value.Should().Be("Card with id 123 was not found.");
    }

    [Fact]
    public async Task GetCardBalanceInCurrency_WhenCardExists_ReturnsConvertedAvailableBalance()
    {
        await using var database = CreateDatabase();

        var card = new Card
        {
            LimitAmount = 1000
        };

        database.Cards.Add(card);
        await database.SaveChangesAsync();

        database.Transactions.AddRange(
            new Transaction
            {
                CardId = card.Id,
                Description = "First purchase",
                Date = new DateTime(2025, 1, 1),
                Amount = 100
            },
            new Transaction
            {
                CardId = card.Id,
                Description = "Second purchase",
                Date = new DateTime(2025, 1, 2),
                Amount = 250
            });

        await database.SaveChangesAsync();

        using var httpClient = CreateHttpClient("""
        {
          "data": [
            {
              "record_date": "2025-01-01",
              "country": "Canada",
              "currency": "Dollar",
              "country_currency_desc": "Canada-Dollar",
              "exchange_rate": "1.5"
            }
          ]
        }
        """);

        var exchangeRatesService = new ExchangeRatesService(httpClient, CreateEmptyConfiguration());

        var result = await CardsApi.GetCardBalanceInCurrency(
            new CardBalanceRequest(card.Id, "Dollar", "Canada"),
            exchangeRatesService,
            database,
            CancellationToken.None);

        var ok = result.Should().BeOfType<Ok<CardBalanceResponse>>().Subject;

        ok.Value.Should().BeEquivalentTo(new CardBalanceResponse(
            card.Id,
            1500,
            975,
            "Dollar",
            "Canada"));
    }

    [Fact]
    public async Task GetCardBalanceInCurrency_WhenCardDoesNotExist_ReturnsNotFound()
    {
        await using var database = CreateDatabase();

        using var httpClient = CreateHttpClient("""
        {
          "data": []
        }
        """);

        var exchangeRatesService = new ExchangeRatesService(httpClient, CreateEmptyConfiguration());

        var result = await CardsApi.GetCardBalanceInCurrency(
            new CardBalanceRequest(123, "Dollar", "Canada"),
            exchangeRatesService,
            database,
            CancellationToken.None);

        var notFound = result.Should().BeOfType<NotFound<string>>().Subject;

        notFound.Value.Should().Be("Card with id 123 was not found.");
    }

    [Fact]
    public async Task GetCardBalanceInCurrency_WhenExchangeRateIsUnavailable_ReturnsBadRequest()
    {
        await using var database = CreateDatabase();

        var card = new Card
        {
            LimitAmount = 1000
        };

        database.Cards.Add(card);
        await database.SaveChangesAsync();

        using var httpClient = CreateHttpClient("""
        {
          "data": []
        }
        """);

        var exchangeRatesService = new ExchangeRatesService(httpClient, CreateEmptyConfiguration());

        var result = await CardsApi.GetCardBalanceInCurrency(
            new CardBalanceRequest(card.Id, "Dollar", "Canada"),
            exchangeRatesService,
            database,
            CancellationToken.None);

        var badRequest = result.Should().BeOfType<BadRequest<string>>().Subject;

        badRequest.Value.Should().Be("Could not get exchange rate for currency.");
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