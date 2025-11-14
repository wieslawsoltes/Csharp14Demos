using System;
using FunctionalExtensions;

namespace FunctionalExtensions.Snippets.Concepts;

public static class ResultConcepts
{
    #region result_try
    public static Result<decimal> ParsePrice(string raw)
        => Result.Try(() =>
        {
            if (!decimal.TryParse(raw, out var value))
            {
                throw new FormatException($"Could not parse '{raw}'.");
            }

            return value;
        });
    #endregion

    #region result_bind
    public static Result<Invoice> BuildInvoice(string rawPrice, SalesOrder order, IExchangeRates rates)
    {
        return ParsePrice(rawPrice)
            .Bind(amount => ConvertToCurrency(amount, order.Currency, "USD", rates))
            .Map(usd => new Invoice(order.Id, usd, "USD"));
    }
    #endregion

    #region result_recover
    public static Result<Invoice> FallbackInvoice(Result<Invoice> invoice, Invoice placeholder)
        => invoice.Recover(_ => placeholder);
    #endregion

    #region result_match
    public static string Summarize(Result<Invoice> invoice)
        => invoice.Match(
            onOk: doc => $"Invoice {doc.Id} ready for {doc.Currency} {doc.Amount:F2}",
            onError: error => $"Invoice failed: {error}");
    #endregion

    private static Result<decimal> ConvertToCurrency(
        decimal amount,
        string fromCurrency,
        string toCurrency,
        IExchangeRates rates)
    {
        return rates.TryGetRate(fromCurrency, toCurrency)
            .Map(rate => Math.Round(amount * rate, 2));
    }
}

public interface IExchangeRates
{
    Result<decimal> TryGetRate(string sourceCurrency, string targetCurrency);
}

public sealed record SalesOrder(Guid Id, string Currency);
public sealed record Invoice(Guid Id, decimal Amount, string Currency);
