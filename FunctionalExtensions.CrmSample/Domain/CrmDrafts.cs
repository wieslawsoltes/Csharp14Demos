using System.Collections.Immutable;
using FunctionalExtensions;
using FunctionalExtensions.CrmSample.Domain;
using FunctionalExtensions.Optics;
using FunctionalExtensions.ValidationDsl;

namespace FunctionalExtensions.CrmSample.Domain;

public sealed record CustomerDraft(
    Option<CustomerId> Id,
    string Name,
    string Email,
    Option<string> SecondaryEmail,
    string Phone,
    string? AddressLine1,
    string? AddressLine2,
    string City,
    string Country,
    string PostalCode,
    Option<string> PreferredChannel,
    bool IsArchived,
    ImmutableArray<AttachmentDraft> Attachments)
{
    public static CustomerDraft FromCustomer(Customer customer)
    {
        var address = customer.Contact.Address;
        var line1 = address.HasValue ? address.Value!.Line1 : string.Empty;
        var line2 = address.HasValue ? address.Value!.Line2 : null;
        var city = address.HasValue ? address.Value!.City : string.Empty;
        var country = address.HasValue ? address.Value!.Country : string.Empty;
        var postal = address.HasValue ? address.Value!.PostalCode : string.Empty;

        return new CustomerDraft(
            Option<CustomerId>.Some(customer.Id),
            customer.Name,
            customer.Email,
            customer.SecondaryEmail,
            customer.Contact.Phone,
            line1,
            line2,
            city,
            country,
            postal,
            customer.Contact.PreferredChannel,
            customer.IsArchived,
            ImmutableArray<AttachmentDraft>.Empty);
    }

    public static CustomerDraft Empty => new(
        Option<CustomerId>.None,
        string.Empty,
        string.Empty,
        Option<string>.None,
        string.Empty,
        string.Empty,
        null,
        string.Empty,
        string.Empty,
        string.Empty,
        Option<string>.None,
        false,
        ImmutableArray<AttachmentDraft>.Empty);
}

public sealed record AttachmentDraft(string FileName, string SourcePath);

public static class CustomerDraftValidator
{
    public static readonly Lens<CustomerDraft, string> NameLens =
        Lens.From<CustomerDraft, string>(draft => draft.Name, static (draft, value) => draft with { Name = value });

    public static readonly Lens<CustomerDraft, string> EmailLens =
        Lens.From<CustomerDraft, string>(draft => draft.Email, static (draft, value) => draft with { Email = value });

    public static readonly Lens<CustomerDraft, string> PhoneLens =
        Lens.From<CustomerDraft, string>(draft => draft.Phone, static (draft, value) => draft with { Phone = value });

    public static Validator<CustomerDraft> Build()
    {
        var emailValidator = Validator<CustomerDraft>.Empty
            .Ensure(EmailLens, value => value.Contains('@'), "must contain '@'")
            .Ensure(EmailLens, value => value.Length >= 5, "looks too short");

        var baseValidator = Validator<CustomerDraft>.Empty
            .Ensure(NameLens, static value => value.Length >= 3, "Name too short")
            .Ensure(PhoneLens, static value => value.Length is >= 7 and <= 20, "Phone must be 7-20 chars");

        return baseValidator.Append(emailValidator);
    }

    public static Validation<CustomerDraft> Validate(CustomerDraft draft)
        => Build().Apply(draft);
}
