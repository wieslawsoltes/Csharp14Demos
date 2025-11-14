using System.Collections.Immutable;
using System.Numerics;
using FunctionalExtensions;
using FunctionalExtensions.Numerics;
using FunctionalExtensions.Optics;

namespace FunctionalExtensions.CrmSample.Domain;

public readonly record struct CustomerId(Guid Value)
{
    public static CustomerId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public sealed record Customer(
    CustomerId Id,
    string Name,
    string Email,
    Option<string> SecondaryEmail,
    ContactInfo Contact,
    LeadScore Score,
    ImmutableArray<Activity> Activities,
    ImmutableArray<CustomerAttachment> Attachments,
    bool IsArchived)
{
    public static Customer CreateNew(string name, string primaryEmail)
        => new(
            CustomerId.New(),
            name,
            primaryEmail,
            Option<string>.None,
            ContactInfo.Empty,
            LeadScore.Zero,
            ImmutableArray<Activity>.Empty,
            ImmutableArray<CustomerAttachment>.Empty,
            false);

    public override string ToString() => $"{Name} ({Email})";
}

public sealed record ContactInfo(
    string Phone,
    Option<Address> Address,
    Option<string> PreferredChannel)
{
    public static ContactInfo Empty => new(string.Empty, Option<Address>.None, Option<string>.None);
}

public sealed record Address(string Line1, string? Line2, string City, string Country, string PostalCode)
{
    public override string ToString()
        => string.IsNullOrWhiteSpace(Line2)
            ? $"{Line1}, {City}, {Country} {PostalCode}"
            : $"{Line1}, {Line2}, {City}, {Country} {PostalCode}";
}

public sealed record Activity(
    Guid Id,
    ActivityType Type,
    DateTimeOffset OccurredAt,
    string Summary,
    Option<TimeSpan> Duration)
{
    public static Activity New(ActivityType type, string summary, Option<TimeSpan> duration, DateTimeOffset now)
        => new(Guid.NewGuid(), type, now, summary, duration);
}

public enum ActivityType
{
    Note,
    Call,
    Meeting,
    Document,
    Sync
}

public sealed record CustomerAttachment(Guid Id, string FileName, string PhysicalPath, long Size, DateTimeOffset AddedAt);

public sealed record LeadScore(Rational<decimal> Value, Complex Trend)
{
    public static LeadScore Zero => new(Rational<decimal>.Zero, Complex.Zero);

    public LeadScore Add(decimal numerator, decimal denominator, Complex delta)
    {
        var contribution = new Rational<decimal>(numerator, denominator);
        var next = Value + contribution;
        var nextTrend = Trend + delta;
        return new(next, nextTrend);
    }

    public decimal Normalized => Value.Value;
    public double Momentum => Trend.Magnitude;
}

public static class CustomerLenses
{
    public static readonly Lens<Customer, string> Name =
        Lens.From<Customer, string>(c => c.Name, static (customer, value) => customer with { Name = value });

    public static readonly Lens<Customer, string> Email =
        Lens.From<Customer, string>(c => c.Email, static (customer, value) => customer with { Email = value });

    public static readonly Lens<Customer, ContactInfo> Contact =
        Lens.From<Customer, ContactInfo>(c => c.Contact, static (customer, value) => customer with { Contact = value });

    public static readonly Lens<ContactInfo, Option<Address>> Address =
        Lens.From<ContactInfo, Option<Address>>(c => c.Address, static (contact, value) => contact with { Address = value });

    public static readonly Lens<Customer, ImmutableArray<Activity>> Activities =
        Lens.From<Customer, ImmutableArray<Activity>>(c => c.Activities, static (customer, value) => customer with { Activities = value });

    public static readonly Lens<Customer, ImmutableArray<CustomerAttachment>> Attachments =
        Lens.From<Customer, ImmutableArray<CustomerAttachment>>(c => c.Attachments, static (customer, value) => customer with { Attachments = value });
}
