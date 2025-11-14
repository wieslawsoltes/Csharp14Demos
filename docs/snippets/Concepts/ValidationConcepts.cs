using System;
using FunctionalExtensions;
using FunctionalExtensions.ValidationDsl;

namespace FunctionalExtensions.Snippets.Concepts;

public static class ValidationConcepts
{
    #region validation_validator
    public static Validator<OrderDraft> DraftValidator { get; } = Validator<OrderDraft>.Empty
        .Ensure(d => !string.IsNullOrWhiteSpace(d.CustomerId), "CustomerId is required.")
        .Ensure(d => d.Total > 0, "Total must be positive.")
        .Ensure(d => d.Total <= 50_000, "Draft exceeds credit policy.")
        .Ensure(d => d.Currency?.Length == 3, "Currency must follow ISO-4217 (3 letters).");
    #endregion

    #region validation_apply
    public static Validation<OrderDraft> ValidateDraft(OrderDraft draft)
        => DraftValidator.Apply(draft);
    #endregion

    #region validation_applicative
    public static Validation<ValidatedOrder> BuildOrder(OrderDraft draft)
    {
        var amount = ValidateAmount(draft.Total);
        var currency = ValidateCurrency(draft.Currency);

        var constructor = amount
            .Map(validAmount => (Func<string, ValidatedOrder>)(validCurrency =>
                new ValidatedOrder(draft.CustomerId, validAmount, validCurrency)));

        return currency.Apply(constructor);
    }
    #endregion

    #region validation_to_result
    public static Result<ValidatedOrder> RequireOrder(OrderDraft draft)
        => BuildOrder(draft).ToResult("Draft failed validation.");
    #endregion

    private static Validation<decimal> ValidateAmount(decimal amount)
        => amount > 0
            ? Validation<decimal>.Success(amount)
            : Validation<decimal>.Failure("Amount must be positive.");

    private static Validation<string> ValidateCurrency(string? currency)
        => string.IsNullOrWhiteSpace(currency) || currency.Length != 3
            ? Validation<string>.Failure("Currency must be a 3-letter ISO code.")
            : Validation<string>.Success(currency.ToUpperInvariant());
}

public sealed record OrderDraft(string CustomerId, decimal Total, string Currency);
public sealed record ValidatedOrder(string CustomerId, decimal Amount, string Currency);
