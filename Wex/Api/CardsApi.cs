using Microsoft.EntityFrameworkCore;
using Wex.Model;
using Wex.Service;

namespace Wex.Api;

public static class CardsApi
{
    public static async Task<IResult> CreateCard(
        CreateCardRequest request,
        Database database,
        CancellationToken cancellationToken)
    {
        if (request.LimitAmount <= 0)
        {
            return Results.BadRequest("LimitAmount must be greater than zero.");
        }

        var card = new Card
        {
            LimitAmount = request.LimitAmount
        };

        database.Cards.Add(card);
        await database.SaveChangesAsync(cancellationToken);

        var response = new CardResponse(
            card.Id,
            card.LimitAmount
        );

        return Results.Created($"/cards/{card.Id}", response);
    }

    public static async Task<IResult> GetCard(
        int id,
        Database database,
        CancellationToken cancellationToken)
    {
        var card = await database.Cards.FindAsync(id, cancellationToken);
        if (card == null)
        {
            return Results.NotFound($"Card with id {id} was not found.");
        }

        return Results.Ok(new CardResponse(card.Id, card.LimitAmount));
    }

    public static async Task<IResult> GetCardBalanceInCurrency(
        CardBalanceRequest request,
        ExchangeRatesService exchangeRatesService,
        Database database,
        CancellationToken cancellationToken)
    {
        var card = await database.Cards.FindAsync(request.Id, cancellationToken);
        if (card == null)
        {
            return Results.NotFound($"Card with id {request.Id} was not found.");
        }

        var totalUsed = await database.Transactions
            .Where(transaction => transaction.CardId == request.Id)
            .SumAsync(transaction => transaction.Amount, cancellationToken);

        var balance = card.LimitAmount - totalUsed;
        var fxRate = await exchangeRatesService.GetLatestExchangeRate(request.Currency, request.Country, cancellationToken);
        if (fxRate == null)
        {
            return Results.BadRequest("Could not get exchange rate for currency.");
        }

        var convertedBalance = balance * (decimal)fxRate;
        var convertedLimit = card.LimitAmount * (decimal)fxRate;
        return Results.Ok(new CardBalanceResponse(request.Id, convertedLimit, convertedBalance, request.Currency,
            request.Country));
    }
}

public record CreateCardRequest(decimal LimitAmount);

public record CardResponse(int Id, decimal LimitAmount);

public record CardBalanceRequest(int Id, string Currency, string Country);

public record CardBalanceResponse(int Id, decimal LimitAmount, decimal Balance, string Currency, string Country);