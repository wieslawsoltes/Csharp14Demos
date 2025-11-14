using System;
using FunctionalExtensions;

namespace FunctionalExtensions.CrmSample.Domain;

public sealed record CrmDomainEvent(string Kind, string Description, DateTimeOffset OccurredAt, Option<CustomerId> CustomerId)
{
    public static CrmDomainEvent CustomerSaved(Customer customer)
        => new("customer:saved", $"Saved {customer.Name}", DateTimeOffset.UtcNow, Option<CustomerId>.Some(customer.Id));

    public static CrmDomainEvent AttachmentImported(CustomerAttachment attachment, CustomerId id)
        => new("attachment:imported", $"Attached {attachment.FileName}", DateTimeOffset.UtcNow, Option<CustomerId>.Some(id));

    public static CrmDomainEvent ActivityLogged(Activity activity, CustomerId id)
        => new("activity:logged", $"{activity.Type} for {id}", DateTimeOffset.UtcNow, Option<CustomerId>.Some(id));

    public static CrmDomainEvent CustomerDeleted(Customer customer)
        => new("customer:deleted", $"Deleted {customer.Name}", DateTimeOffset.UtcNow, Option<CustomerId>.Some(customer.Id));

    public static CrmDomainEvent AttachmentRemoved(CustomerAttachment attachment, CustomerId id)
        => new("attachment:removed", $"Removed {attachment.FileName}", DateTimeOffset.UtcNow, Option<CustomerId>.Some(id));
}
