using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using FunctionalExtensions;
using FunctionalExtensions.Computation;
using FunctionalExtensions.CrmSample.Domain;
using FunctionalExtensions.CrmSample.Infrastructure.Http;
using FunctionalExtensions.CrmSample.Infrastructure.Notifications;
using FunctionalExtensions.TypeClasses;
using System.Linq;
using FunctionalExtensions.Optics;

namespace FunctionalExtensions.CrmSample.Runtime;

public static class CrmWorkflows
{
    public static ReaderTaskResult<CrmEnvironment, IReadOnlyList<Customer>> LoadCustomers(CancellationToken cancellationToken = default)
        => ReaderTaskResults.From<CrmEnvironment, IReadOnlyList<Customer>>(env =>
            env.Database
                .LoadCustomersAsync(cancellationToken)
                .Tap(customers => env.Notifications.PublishAsync(CrmNotification.Info($"Loaded {customers.Count} customers at {DateTimeOffset.Now:t}")).Invoke()));

    public static ReaderTaskResult<CrmEnvironment, Writer<Customer, CrmDomainEvent>> SaveCustomer(CustomerDraft draft, CancellationToken cancellationToken = default)
        => ReaderTaskResults.From<CrmEnvironment, Writer<Customer, CrmDomainEvent>>(env =>
            TaskResults.Do.Run(
                (Func<TaskResultDoScope, Task<Writer<Customer, CrmDomainEvent>>>)(async scope =>
                {
                    var validated = await scope.Await(CustomerDraftValidator.Validate(draft).ToTaskResult("Draft was invalid."));
                    var normalized = NormalizeDraft(validated);
                    var customer = BuildCustomer(normalized);

                    await scope.Await(env.Database.UpsertCustomerAsync(customer, cancellationToken));

                    var attachments = ImmutableArray.CreateBuilder<CustomerAttachment>();
                    foreach (var pending in normalized.Attachments.Filter(static attachment => !string.IsNullOrWhiteSpace(attachment.SourcePath)))
                    {
                        var attachment = await scope.Await(env.FileStore.ImportAsync(pending));
                        attachments.Add(attachment);
                    }

                    var immutableAttachments = attachments.ToImmutable();
                    if (immutableAttachments.Length > 0)
                    {
                        await scope.Await(env.Database.UpsertAttachmentsAsync(customer.Id, immutableAttachments, cancellationToken));
                    }

                    env.Undo.Push(IO.From(() =>
                    {
                        env.Database.ArchiveCustomerAsync(customer.Id, true).Invoke().GetAwaiter().GetResult();
                        return Unit.Value;
                    }));

                    var logs = new List<CrmDomainEvent>
                    {
                        CrmDomainEvent.CustomerSaved(customer)
                    };

                    logs.AddRange(immutableAttachments.Select(attachment => CrmDomainEvent.AttachmentImported(attachment, customer.Id)));

                    await scope.Await(env.Notifications.PublishAsync(CrmNotification.Success($"Saved {customer.Name}")));

                    var enrichedCustomer = immutableAttachments.Length > 0
                        ? customer with { Attachments = immutableAttachments }
                        : customer;

                    return Writer.From(enrichedCustomer, logs.ToArray());
                })));

    public static ReaderTaskResult<CrmEnvironment, Option<CustomerDraft>> LoadDraft(CustomerId id, CancellationToken cancellationToken = default)
        => ReaderTaskResults.From<CrmEnvironment, Option<CustomerDraft>>(env =>
            env.Database
                .TryLoadCustomerAsync(id, cancellationToken)
                .Map(option => option.HasValue
                    ? Option<CustomerDraft>.Some(CustomerDraft.FromCustomer(option.Value!))
                    : Option<CustomerDraft>.None));

    public static ReaderTaskResult<CrmEnvironment, Writer<Unit, CrmDomainEvent>> DeleteCustomer(CustomerId id, CancellationToken cancellationToken = default)
        => ReaderTaskResults.From<CrmEnvironment, Writer<Unit, CrmDomainEvent>>(env =>
            TaskResults.Do.Run(
                (Func<TaskResultDoScope, Task<Writer<Unit, CrmDomainEvent>>>)(async scope =>
                {
                    var customerOption = await scope.Await(env.Database.TryLoadCustomerAsync(id, cancellationToken));
                    scope.Ensure(customerOption.HasValue, $"Customer {id} not found.");
                    var customer = customerOption.Value!;

                    var attachments = await scope.Await(env.Database.LoadAttachmentsAsync(id, cancellationToken));
                    await scope.Await(env.Database.DeleteCustomerAsync(id, cancellationToken));

                    foreach (var attachment in attachments)
                    {
                        await scope.Await(env.FileStore.DeleteAsync(attachment));
                    }

                    await scope.Await(env.Notifications.PublishAsync(CrmNotification.Success($"Deleted {customer.Name}.")));

                    var logs = new List<CrmDomainEvent>
                    {
                        CrmDomainEvent.CustomerDeleted(customer)
                    };
                    logs.AddRange(System.Linq.Enumerable.Select(attachments, attachment => CrmDomainEvent.AttachmentRemoved(attachment, customer.Id)));

                    return Writer.From(Unit.Value, logs.ToArray());
                })));

    public static ReaderTaskResult<CrmEnvironment, IReadOnlyList<RemoteCompany>> DownloadRemoteCompanies(CancellationToken cancellationToken = default)
        => ReaderTaskResults.From<CrmEnvironment, IReadOnlyList<RemoteCompany>>(env =>
            env.EnrichmentClient
                .DownloadCompaniesAsync(cancellationToken)
                .Tap(companies => env.Notifications.PublishAsync(CrmNotification.Info($"Synced {companies.Count} remote companies")).Invoke()));

    public static ReaderTaskResult<CrmEnvironment, Result<Unit>> UndoLastOperation()
        => ReaderTaskResults.From<CrmEnvironment, Result<Unit>>(env =>
            TaskResults.Return(env.Undo.RunUndo()));

    private static CustomerDraft NormalizeDraft(CustomerDraft draft)
        => draft with
        {
            Name = draft.Name.Trim(),
            Email = draft.Email.Trim().ToLowerInvariant(),
            SecondaryEmail = draft.SecondaryEmail.Bind(static email =>
                string.IsNullOrWhiteSpace(email)
                    ? Option<string>.None
                    : Option<string>.Some(email.Trim())),
            Phone = draft.Phone.Trim(),
            City = draft.City.Trim(),
            Country = draft.Country.Trim(),
            PostalCode = draft.PostalCode.Trim()
        };

    private static Customer BuildCustomer(CustomerDraft draft)
    {
        var baseCustomer = draft.Id.HasValue
            ? new Customer(
                draft.Id.Value,
                draft.Name,
                draft.Email,
                draft.SecondaryEmail,
                ContactInfo.Empty,
                LeadScore.Zero,
                ImmutableArray<Activity>.Empty,
                ImmutableArray<CustomerAttachment>.Empty,
                draft.IsArchived)
            : Customer.CreateNew(draft.Name, draft.Email);

        var withName = CustomerLenses.Name.Set(baseCustomer, draft.Name);
        var withEmail = CustomerLenses.Email.Set(withName, draft.Email);

        var addressOption = string.IsNullOrWhiteSpace(draft.AddressLine1)
            ? Option<Address>.None
            : Option<Address>.Some(new Address(draft.AddressLine1!, draft.AddressLine2, draft.City, draft.Country, draft.PostalCode));

        var derivedChannel = draft.PreferredChannel.LiftA2(
            draft.SecondaryEmail,
            static (primary, secondary) => $"{primary} / backup: {secondary}");

        var channel = derivedChannel.HasValue ? derivedChannel : draft.PreferredChannel;

        var contact = new ContactInfo(
            draft.Phone,
            addressOption,
            channel);

        var updatedContact = CustomerLenses.Contact.Set(withEmail, contact);

        var attachments = System.Linq.Enumerable.Select(
                draft.Attachments,
                static attachment => new CustomerAttachment(Guid.NewGuid(), attachment.FileName, attachment.SourcePath, 0, DateTimeOffset.UtcNow))
            .ToImmutableArray();

        var updatedScore = updatedContact.Score.Add(
            numerator: draft.Attachments.Length + draft.Phone.Length,
            denominator: Math.Max(1, draft.Name.Length),
            delta: new Complex(draft.Email.Length, draft.Phone.Length * 0.1));

        return updatedContact with
        {
            SecondaryEmail = draft.SecondaryEmail,
            Attachments = attachments,
            Score = updatedScore
        };
    }
}
