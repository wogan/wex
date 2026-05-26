using System.Globalization;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.WebUtilities;

namespace Wex.Service;

/*
Currency conversion requirements:
   When converting between currencies, you do not need an exact date match, but you must use a currency
    conversion rate dated on or before the transaction date from within the prior 6 months.
   If no currency conversion rate is available within 6 months on or before the transaction date, an
    error should be returned stating the transaction cannot be converted to the target currency.
 */

public class ExchangeRatesService(HttpClient httpClient, IConfiguration configuration)
{
    private string ExchangeRatesUrl => configuration["ExchangeRates:BaseUrl"] ??
        "https://api.fiscaldata.treasury.gov/services/api/fiscal_service/v1/accounting/od/rates_of_exchange";

    private int HistoricalExchangeRateLookbackDays =>
        configuration.GetValue("ExchangeRates:HistoricalLookbackDays", 180);

    public async Task<decimal?> GetLatestExchangeRate(
        String currency,
        String country,
        CancellationToken cancellationToken = default)
    {
        return await GetExchangeRate(currency, country, null, null, cancellationToken);
    }
    
    public async Task<decimal?> GetExchangeRateForDate(
        String currency,
        String country,
        DateTimeOffset date,
        CancellationToken cancellationToken = default)
    {
        var startDate = date - TimeSpan.FromDays(HistoricalExchangeRateLookbackDays);
        var endDateText = date.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var startDateText = startDate.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        
        return await GetExchangeRate(currency, country, startDateText, endDateText, cancellationToken);
    }

    private async Task<decimal?> GetExchangeRate(
        string currency,
        string country,
        string? startDateText,
        string? endDateText,
        CancellationToken cancellationToken)
    {
        var filter = $"country_currency_desc:eq:{country}-{currency}";
        if (startDateText != null && endDateText != null)
        {
            filter += $",effective_date:lte:{endDateText},effective_date:gte:{startDateText}";
        }

        var querystring = new Dictionary<string, string?>
        {
            { "sort", "-record_date" }, // most recent first
            { "format", "json" },
            { "fields", "record_date,country,currency,country_currency_desc,exchange_rate" },
            { "filter", filter },
            { "page[number]", "1" },
            { "page[size]", "1" } // only need one record
        };
        var uri = QueryHelpers.AddQueryString(ExchangeRatesUrl, querystring);
        var response = await httpClient.GetFromJsonAsync<TreasuryExchangeRatesResponse>(uri, cancellationToken);
        return response?.Data.FirstOrDefault()?.ExchangeRate;
    }
}

public sealed record TreasuryExchangeRatesResponse(
    [property: JsonPropertyName("data")] IReadOnlyList<ExchangeRateRecord> Data);

public sealed record ExchangeRateRecord(
    [property: JsonPropertyName("record_date")]
    DateOnly RecordDate,
    [property: JsonPropertyName("country")]
    string Country,
    [property: JsonPropertyName("currency")]
    string Currency,
    [property: JsonPropertyName("country_currency_desc")]
    string CountryCurrencyDescription,
    [property: JsonPropertyName("exchange_rate")]
    decimal ExchangeRate);