using System;
using System.Collections.Generic;
using System.Linq;
using FunctionalExtensions;
using FunctionalExtensions.CrmSample.Domain;

namespace FunctionalExtensions.CrmSample.Runtime;

public sealed record DashboardFilter(string Search, bool IncludeArchived)
{
    public static DashboardFilter Default => new(string.Empty, false);
}

public static class DashboardState
{
    public static State<DashboardFilter, IReadOnlyList<Customer>> FilterCustomers(
        IEnumerable<Customer> customers,
        string search,
        bool includeArchived)
        => State.From<DashboardFilter, IReadOnlyList<Customer>>(state =>
        {
            var normalized = search?.Trim() ?? string.Empty;
            var filtered = customers
                .Filter(customer => includeArchived || !customer.IsArchived)
                .Filter(customer => string.IsNullOrWhiteSpace(normalized)
                    || customer.Name.Contains(normalized, StringComparison.OrdinalIgnoreCase)
                    || customer.Email.Contains(normalized, StringComparison.OrdinalIgnoreCase))
                .ToList()
                .AsReadOnly();

            var updated = state with
            {
                Search = normalized,
                IncludeArchived = includeArchived
            };

            return (filtered, updated);
        });
}
