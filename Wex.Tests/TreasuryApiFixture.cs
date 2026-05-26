using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Wex.Tests;

public class TreasuryApiFixture : IDisposable
{
    public WireMockServer Server { get; } = WireMockServer.Start();

    public void SetupExchangeRate(string country, string currency, decimal rate, string recordDate)
    {
        Server
            .Given(Request.Create()
                .WithPath("/rates")
                .WithParam("filter")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($$"""
                    {
                        "data": [
                            {
                                "record_date": "{{recordDate}}",
                                "country": "{{country}}",
                                "currency": "{{currency}}",
                                "country_currency_desc": "{{country}}-{{currency}}",
                                "exchange_rate": "{{rate}}"
                            }
                        ]
                    }
                """));
    }

    public void Reset()
    {
        Server.Reset();
    }

    public void Dispose()
    {
        Server.Stop();
        Server.Dispose();
    }
}
