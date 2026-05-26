using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Wex.Api;

namespace Wex.Tests;

public class ServiceIntegrationTests(WexWebApplicationFactory factory)
    : IClassFixture<WexWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public async Task InitializeAsync()
    {
        await factory.EnsureDatabaseCreatedAsync();
        factory.TreasuryApi.Reset();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateCardAndTransaction_ShouldSucceed()
    {
        // 1. Create a Card
        var createCardRequest = new CreateCardRequest(1000m);
        var createCardResponse = await _client.PostAsJsonAsync("/cards", createCardRequest);
        
        createCardResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var card = await createCardResponse.Content.ReadFromJsonAsync<CardResponse>();
        card.Should().NotBeNull();
        card.LimitAmount.Should().Be(1000m);

        // 2. Create a Transaction for that card
        var createTransactionRequest = new CreateTransactionRequest(
            card.Id,
            100m,
            DateTimeOffset.UtcNow,
            "Integration Test Transaction"
        );
        var createTransactionResponse = await _client.PostAsJsonAsync("/transactions", createTransactionRequest);
        
        createTransactionResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var transaction = await createTransactionResponse.Content.ReadFromJsonAsync<TransactionResponse>();
        transaction.Should().NotBeNull();
        transaction.CardId.Should().Be(card.Id);
        transaction.Amount.Should().Be(100m);

        // 3. Verify Get Transaction works
        var getTransactionResponse = await _client.GetAsync($"/transactions/{transaction.Id}");
        getTransactionResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetchedTransaction = await getTransactionResponse.Content.ReadFromJsonAsync<TransactionResponse>();
        fetchedTransaction.Should().NotBeNull();
        fetchedTransaction.Id.Should().Be(transaction.Id);
        fetchedTransaction.Description.Should().Be("Integration Test Transaction");
    }

    [Fact]
    public async Task GetTransaction_WithCurrencyAndCountry_ShouldReturnConvertedAmount()
    {
        // 1. Create a Card
        var createCardRequest = new CreateCardRequest(1000m);
        var createCardResponse = await _client.PostAsJsonAsync("/cards", createCardRequest);
        var card = await createCardResponse.Content.ReadFromJsonAsync<CardResponse>();

        // 2. Create a Transaction
        var transactionDate = new DateTimeOffset(2023, 10, 27, 0, 0, 0, TimeSpan.Zero);
        var createTransactionRequest = new CreateTransactionRequest(
            card!.Id,
            100m,
            transactionDate,
            "FX Transaction"
        );
        var createTransactionResponse = await _client.PostAsJsonAsync("/transactions", createTransactionRequest);
        var transaction = await createTransactionResponse.Content.ReadFromJsonAsync<TransactionResponse>();

        // 3. Setup WireMock
        factory.TreasuryApi.SetupExchangeRate("Brazil", "Real", 5.00m, "2023-09-30");

        // 4. Get Transaction with FX
        var getTransactionResponse = await _client.GetAsync($"/transactions/{transaction!.Id}?currency=Real&country=Brazil");
        
        getTransactionResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var convertedTransaction = await getTransactionResponse.Content.ReadFromJsonAsync<ConvertedTransactionResponse>();
        
        convertedTransaction.Should().NotBeNull();
        convertedTransaction.OriginalAmount.Should().Be(100m);
        convertedTransaction.ExchangeRate.Should().Be(5.00m);
        convertedTransaction.ConvertedAmount.Should().Be(500m);
        convertedTransaction.Currency.Should().Be("Real");
    }

    [Fact]
    public async Task CardLimitsAndBalances_ShouldBeCorrect()
    {
        // 1. Setup WireMock for exchange rate (1.45 instead of 1.00)
        factory.TreasuryApi.SetupExchangeRate("Australia", "Dollar", 1.45m, DateTimeOffset.UtcNow.ToString("yyyy-MM-dd"));

        // 2. Create 2 Cards
        var cardARequest = new CreateCardRequest(1000m);
        var cardBRequest = new CreateCardRequest(500m);

        var responseA = await _client.PostAsJsonAsync("/cards", cardARequest);
        var responseB = await _client.PostAsJsonAsync("/cards", cardBRequest);

        var cardA = await responseA.Content.ReadFromJsonAsync<CardResponse>();
        var cardB = await responseB.Content.ReadFromJsonAsync<CardResponse>();

        // 3. Create 10 transactions across both cards (6 for A, 4 for B)
        for (int i = 0; i < 6; i++)
        {
            var req = new CreateTransactionRequest(cardA!.Id, 50m, DateTimeOffset.UtcNow, $"Card A Trans {i}");
            await _client.PostAsJsonAsync("/transactions", req);
        }

        for (int i = 0; i < 4; i++)
        {
            var req = new CreateTransactionRequest(cardB!.Id, 25m, DateTimeOffset.UtcNow, $"Card B Trans {i}");
            await _client.PostAsJsonAsync("/transactions", req);
        }

        // 4. Verify Card A Balance
        // Total A used: 6 * 50 = 300. Balance in USD: 1000 - 300 = 700.
        // Converted Limit: 1000 * 1.45 = 1450.
        // Converted Balance: 700 * 1.45 = 1015.
        var balanceAResponse = await _client.GetAsync($"/cards/{cardA!.Id}/balance?currency=Dollar&country=Australia");
        balanceAResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var balanceA = await balanceAResponse.Content.ReadFromJsonAsync<CardBalanceResponse>();
        balanceA.Should().NotBeNull();
        balanceA.LimitAmount.Should().Be(1450m);
        balanceA.Balance.Should().Be(1015m);

        // 5. Verify Card B Balance
        // Total B used: 4 * 25 = 100. Balance in USD: 500 - 100 = 400.
        // Converted Limit: 500 * 1.45 = 725.
        // Converted Balance: 400 * 1.45 = 580.
        var balanceBResponse = await _client.GetAsync($"/cards/{cardB!.Id}/balance?currency=Dollar&country=Australia");
        balanceBResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var balanceB = await balanceBResponse.Content.ReadFromJsonAsync<CardBalanceResponse>();
        balanceB.Should().NotBeNull();
        balanceB.LimitAmount.Should().Be(725m);
        balanceB.Balance.Should().Be(580m);
    }
}
