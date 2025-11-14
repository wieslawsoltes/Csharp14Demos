using System;
using FunctionalExtensions;
using FunctionalExtensions.Optics;
using FunctionalExtensions.ValidationDsl;

namespace FunctionalExtensions.Snippets.HowTo;

public static class OpticsGuide
{
    #region optics_define
    public static Lens<Contact, Address> ContactAddress { get; } =
        Lens.From<Contact, Address>(c => c.Address, (contact, address) => contact with { Address = address });

    public static Lens<Address, Geo> AddressGeo { get; } =
        Lens.From<Address, Geo>(a => a.Geo, (address, geo) => address with { Geo = geo });

    public static Lens<Geo, string> GeoCity { get; } =
        Lens.From<Geo, string>(g => g.City, (geo, city) => geo with { City = city });
    #endregion

    #region optics_compose
    public static Lens<Contact, string> ContactCity { get; } =
        ContactAddress.Compose(AddressGeo).Compose(GeoCity);
    #endregion

    #region optics_over
    public static Contact MoveCustomer(Contact contact, string newCity)
        => ContactCity.Over(contact, _ => newCity.ToUpperInvariant());
    #endregion

    #region optics_describe
    public static string DescribeCityLens()
        => ContactCity.Describe();
    #endregion

    #region optics_validate
    public static Lens<Contact, string> ContactEmail { get; } =
        Lens.From<Contact, string>(c => c.Email, (contact, email) => contact with { Email = email });

    public static Validator<Contact> ContactValidator { get; } = Validator<Contact>.Empty
        .Ensure(ContactCity, city => !string.IsNullOrWhiteSpace(city), "City is required.")
        .Ensure(ContactEmail, email => email.Contains('@'), "Email must contain '@'.");
    #endregion
}

public sealed record Contact(Guid Id, string Name, string Email, Address Address);
public sealed record Address(string Line1, string PostalCode, Geo Geo);
public sealed record Geo(string City, string Country);
