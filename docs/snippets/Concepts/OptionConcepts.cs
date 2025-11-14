using System;
using System.Collections.Generic;
using FunctionalExtensions;

namespace FunctionalExtensions.Snippets.Concepts;

public static class OptionConcepts
{
    #region option_construct
    public static Option<string> NormalizeInput(string? raw)
    {
        var text = raw?.Trim();
        return string.IsNullOrWhiteSpace(text)
            ? Option<string>.None
            : Option<string>.Some(text);
    }
    #endregion

    #region option_pipeline
    public static Option<Email> BuildMarketingEmail(
        IDictionary<Guid, Customer> customers,
        IDictionary<string, string> domains,
        Guid? customerId)
    {
        return Option.FromNullable(customerId)
            .Bind(id => customers.TryGetValue(id, out var customer)
                ? Option<Customer>.Some(customer)
                : Option<Customer>.None)
            .Where(customer => customer.MarketingOptIn)
            .Bind(customer => domains.TryGetValue(customer.PreferredDomain, out var domain)
                ? Option<Email>.Some(new Email($"{Slugify(customer.Name)}@{domain}"))
                : Option<Email>.None);
    }
    #endregion

    #region option_match
    public static string DescribeEmail(Option<Email> email)
        => email.Match(
            whenSome: address => $"Ready to send to {address.Address}",
            whenNone: () => "Subscriber missing required data.");
    #endregion

    #region option_to_result
    public static Result<Email> RequireEmail(Option<Email> email)
        => email.ToResult("Email could not be constructed.");
    #endregion

    private static string Slugify(string value)
        => value
            .Trim()
            .Replace(" ", ".", StringComparison.Ordinal)
            .ToLowerInvariant();
}

public sealed record Customer(Guid Id, string Name, bool MarketingOptIn, string PreferredDomain);
public sealed record Email(string Address);
