using FluentValidation.TestHelper;
using Wex.Api;

namespace Wex.Tests;

public class CreateTransactionRequestValidatorTests
{
    private readonly CreateTransactionRequestValidator _validator = new();

    [Fact]
    public void Validate_WhenRequestIsValid_DoesNotHaveValidationErrors()
    {
        var request = new CreateTransactionRequest(
            CardId: 1,
            Amount: 23.50m,
            Date: DateTimeOffset.UtcNow.AddMinutes(-1),
            Description: "Test transaction");

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WhenCardIdIsNotPositive_HasValidationError(int cardId)
    {
        var request = ValidRequest() with { CardId = cardId };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.CardId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WhenAmountIsNotPositive_HasValidationError(decimal amount)
    {
        var request = ValidRequest() with { Amount = amount };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenDescriptionIsEmpty_HasValidationError(string description)
    {
        var request = ValidRequest() with { Description = description };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validate_WhenDescriptionIsLongerTha255Characters_HasValidationError()
    {
        var request = ValidRequest() with { Description = new string('a', 256) };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validate_WhenDateIsInTheFuture_HasValidationError()
    {
        var request = ValidRequest() with { Date = DateTimeOffset.UtcNow.AddDays(1) };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Date);
    }

    private static CreateTransactionRequest ValidRequest()
    {
        return new CreateTransactionRequest(
            CardId: 1,
            Amount: 23.50m,
            Date: DateTimeOffset.UtcNow.AddMinutes(-1),
            Description: "Test transaction");
    }
}