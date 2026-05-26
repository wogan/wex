using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Wex.Model;
using Wex.Service;

namespace Wex.Api;

public static class TransactionsApi
{
    public static async Task<IResult> CreateTransaction(
        CreateTransactionRequest request,
        IValidator<CreateTransactionRequest> validator,
        Database database,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var cardExists = await database.Cards
            .AnyAsync(card => card.Id == request.CardId, cancellationToken);

        if (!cardExists)
        {
            return Results.NotFound($"Card with id {request.CardId} was not found.");
        }

        var transaction = new Transaction()
        {
            Amount = request.Amount,
            Date = request.Date,
            Description = request.Description,
            CardId = request.CardId
        };

        database.Transactions.Add(transaction);
        await database.SaveChangesAsync(cancellationToken);

        var response = new TransactionResponse(
            transaction.Id,
            transaction.CardId,
            transaction.Amount,
            transaction.Date,
            transaction.Description
        );

        return Results.Created($"/transactions/{transaction.Id}", response);
    }

    public static async Task<IResult> GetTransaction(
        int id,
        string? currency,
        string? country,
        Database database,
        ExchangeRatesService exchangeRatesService,
        CancellationToken cancellationToken)
    {
        var transaction = await database.Transactions
            .AsNoTracking()
            .FirstOrDefaultAsync(transaction => transaction.Id == id, cancellationToken);

        if (transaction is null)
        {
            return Results.NotFound($"Transaction with id {id} was not found.");
        }

        if (currency is null || country is null)
        {
            var simpleResponse = new TransactionResponse(
                transaction.Id,
                transaction.CardId,
                transaction.Amount,
                transaction.Date,
                transaction.Description
            );
            return Results.Ok(simpleResponse);
        }

        var exchangeRate = await exchangeRatesService.GetExchangeRateForDate(
            currency,
            country,
            transaction.Date,
            cancellationToken);

        if (exchangeRate is null)
        {
            return Results.BadRequest(
                $"Transaction {id} cannot be converted to {currency} because no exchange rate is available within 6 months on or before the transaction date.");
        }

        var response = new ConvertedTransactionResponse(
            transaction.Id,
            transaction.Description,
            transaction.Date,
            transaction.Amount,
            exchangeRate.Value,
            transaction.Amount * exchangeRate.Value,
            currency
        );

        return Results.Ok(response);
    }
}

public record CreateTransactionRequest(int CardId, decimal Amount, DateTimeOffset Date, string Description);
public record TransactionResponse(int Id, int CardId, decimal Amount, DateTimeOffset Date, string Description);
public record ConvertedTransactionResponse(
    int Id,
    string Description,
    DateTimeOffset Date,
    decimal OriginalAmount,
    decimal ExchangeRate,
    decimal ConvertedAmount,
    string Currency);
