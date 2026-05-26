using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Wex.Service;

namespace Wex.Tests;

public class ExchangeRatesServiceTests
{
    private static IConfiguration CreateEmptyConfiguration() =>
        new ConfigurationBuilder().Build();

    private static IConfiguration CreateConfiguration(
        params KeyValuePair<string, string?>[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

    [Fact]
    public async Task GetExchangeRateForDate_WhenHistoricalLookbackDaysIsConfigured_UsesConfiguredLookback()
    {
        const string json = """
        {
          "data": []
        }
        """;

        var handler = new StubHttpMessageHandler(json);
        using var httpClient = new HttpClient(handler);
        var service = new ExchangeRatesService(
            httpClient,
            CreateConfiguration(new KeyValuePair<string, string?>(
                "ExchangeRates:HistoricalLookbackDays",
                "30")));

        await service.GetExchangeRateForDate(
            "Dollar",
            "Canada",
            new DateTimeOffset(2025, 1, 15, 0, 0, 0, TimeSpan.Zero));

        handler.RequestUri.Should().NotBeNull();

        var queryParameters = QueryHelpers.ParseQuery(handler.RequestUri!.Query);

        queryParameters["filter"].Should().ContainSingle().Which.Should().Be(
            "country_currency_desc:eq:Canada-Dollar,effective_date:lte:2025-01-15,effective_date:gte:2024-12-16");
    }

    [Fact]
    public async Task GetExchangeRateForDate_WhenRateExists_ReturnsExchangeRate()
    {
        const string json = """
        {
          "data": [
            {
              "record_date": "2024-12-31",
              "country": "Canada",
              "currency": "Dollar",
              "country_currency_desc": "Canada-Dollar",
              "exchange_rate": "1.438"
            }
          ]
        }
        """;

        using var httpClient = CreateHttpClient(json);
        var service = new ExchangeRatesService(httpClient, CreateEmptyConfiguration());

        var result = await service.GetExchangeRateForDate(
            "Dollar",
            "Canada",
            new DateTimeOffset(2025, 1, 15, 0, 0, 0, TimeSpan.Zero));

        result.Should().Be(1.438m);
    }

    [Fact]
    public async Task GetExchangeRateForDate_WhenResponseDataIsEmpty_ReturnsNull()
    {
        const string json = """
        {
          "data": []
        }
        """;

        using var httpClient = CreateHttpClient(json);
        var service = new ExchangeRatesService(httpClient, CreateEmptyConfiguration());

        var result = await service.GetExchangeRateForDate(
            "Dollar",
            "Canada",
            new DateTimeOffset(2025, 1, 15, 0, 0, 0, TimeSpan.Zero));

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetExchangeRateForDate_CallsExpectedTreasuryEndpoint()
    {
        const string json = """
        {
          "data": []
        }
        """;

        var handler = new StubHttpMessageHandler(json);
        using var httpClient = new HttpClient(handler);
        var service = new ExchangeRatesService(httpClient, CreateEmptyConfiguration());

        await service.GetExchangeRateForDate(
            "Dollar",
            "Canada",
            new DateTimeOffset(2025, 1, 15, 0, 0, 0, TimeSpan.Zero));

        handler.Method.Should().Be(HttpMethod.Get);
        handler.RequestUri.Should().NotBeNull();

        handler.RequestUri!.GetLeftPart(UriPartial.Path).Should().Be(
            "https://api.fiscaldata.treasury.gov/services/api/fiscal_service/v1/accounting/od/rates_of_exchange");

        var queryParameters = QueryHelpers.ParseQuery(handler.RequestUri.Query);

        queryParameters["sort"].Should().ContainSingle().Which.Should().Be("-record_date");
        queryParameters["format"].Should().ContainSingle().Which.Should().Be("json");
        queryParameters["fields"].Should().ContainSingle().Which.Should().Be(
            "record_date,country,currency,country_currency_desc,exchange_rate");
        queryParameters["filter"].Should().ContainSingle().Which.Should().Be(
            "country_currency_desc:eq:Canada-Dollar,effective_date:lte:2025-01-15,effective_date:gte:2024-07-19");
        queryParameters["page[number]"].Should().ContainSingle().Which.Should().Be("1");
        queryParameters["page[size]"].Should().ContainSingle().Which.Should().Be("1");
    }

    [Fact]
    public async Task GetExchangeRateForDate_WhenApiReturnsNullJson_ReturnsNull()
    {
        const string json = "null";

        using var httpClient = CreateHttpClient(json);
        var service = new ExchangeRatesService(httpClient, CreateEmptyConfiguration());

        var result = await service.GetExchangeRateForDate(
            "Dollar",
            "Canada",
            new DateTimeOffset(2025, 1, 15, 0, 0, 0, TimeSpan.Zero));

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetLatestExchangeRate_WhenRateExists_ReturnsExchangeRate()
    {
        const string json = """
        {
          "data": [
            {
              "record_date": "2024-12-31",
              "country": "Canada",
              "currency": "Dollar",
              "country_currency_desc": "Canada-Dollar",
              "exchange_rate": "1.438"
            }
          ]
        }
        """;

        using var httpClient = CreateHttpClient(json);
        var service = new ExchangeRatesService(httpClient, CreateEmptyConfiguration());

        var result = await service.GetLatestExchangeRate(
            "Dollar",
            "Canada");

        result.Should().Be(1.438m);
    }

    [Fact]
    public async Task GetLatestExchangeRate_CallsExpectedTreasuryEndpoint()
    {
        const string json = """
        {
          "data": []
        }
        """;

        var handler = new StubHttpMessageHandler(json);
        using var httpClient = new HttpClient(handler);
        var service = new ExchangeRatesService(httpClient, CreateEmptyConfiguration());

        await service.GetLatestExchangeRate(
            "Dollar",
            "Canada");

        handler.Method.Should().Be(HttpMethod.Get);
        handler.RequestUri.Should().NotBeNull();

        handler.RequestUri!.GetLeftPart(UriPartial.Path).Should().Be(
            "https://api.fiscaldata.treasury.gov/services/api/fiscal_service/v1/accounting/od/rates_of_exchange");

        var queryParameters = QueryHelpers.ParseQuery(handler.RequestUri.Query);

        queryParameters["sort"].Should().ContainSingle().Which.Should().Be("-record_date");
        queryParameters["format"].Should().ContainSingle().Which.Should().Be("json");
        queryParameters["fields"].Should().ContainSingle().Which.Should().Be(
            "record_date,country,currency,country_currency_desc,exchange_rate");
        queryParameters["filter"].Should().ContainSingle().Which.Should().Be("country_currency_desc:eq:Canada-Dollar");
        queryParameters["page[number]"].Should().ContainSingle().Which.Should().Be("1");
        queryParameters["page[size]"].Should().ContainSingle().Which.Should().Be("1");
    }

    private static HttpClient CreateHttpClient(string json)
    {
        return new HttpClient(new StubHttpMessageHandler(json));
    }

    private sealed class StubHttpMessageHandler(string responseBody) : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }
        public HttpMethod? Method { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            Method = request.Method;

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody)
            };

            return Task.FromResult(response);
        }
    }
}