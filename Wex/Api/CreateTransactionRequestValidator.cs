using FluentValidation;

namespace Wex.Api;

public class CreateTransactionRequestValidator : AbstractValidator<CreateTransactionRequest>
{
    public CreateTransactionRequestValidator()
    {
        RuleFor(request => request.CardId)
            .GreaterThan(0);

        RuleFor(request => request.Amount)
            .GreaterThan(0);

        RuleFor(request => request.Description)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(request => request.Date)
            .LessThanOrEqualTo(DateTimeOffset.UtcNow);
    }
}