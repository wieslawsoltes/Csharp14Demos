# How-To: Model Complex Updates with Lenses

Lenses encapsulate “focus” into nested immutable objects—read a value, update it, or compose deeper lenses without manual copying. FunctionalExtensions implements lenses via C# 14 extension members so they feel native inside your codebase. This guide walks through defining lenses, composing them, performing updates, and reusing the same optics in validation.

All snippets compile in `docs/snippets/HowTo/OpticsGuide.cs`.

## 1. Define reusable lenses
Create lenses with `Lens.From` (captures path information) or `Lens.Create` (manual getter/setter). Use them as singletons so they can be shared across services, validators, and UI layers.

```csharp
public static Lens<Contact, Address> ContactAddress { get; } =
    Lens.From<Contact, Address>(c => c.Address, (contact, address) => contact with { Address = address });

public static Lens<Address, Geo> AddressGeo { get; } =
    Lens.From<Address, Geo>(a => a.Geo, (address, geo) => address with { Geo = geo });

public static Lens<Geo, string> GeoCity { get; } =
    Lens.From<Geo, string>(g => g.City, (geo, city) => geo with { City = city });
```
_Snippet: `OpticsGuide.cs#region optics_define`_

Each lens stores a getter, setter, and optional path (`Contact.Address.Geo.City`) used for diagnostics.

## 2. Compose deeper focuses
Use `Compose` to zoom from the root object down to a primitive—no intermediate plumbing required.

```csharp
public static Lens<Contact, string> ContactCity { get; } =
    ContactAddress.Compose(AddressGeo).Compose(GeoCity);
```
_Snippet: `OpticsGuide.cs#region optics_compose`_

Now you have a single lens that can read/update the city for any `Contact`.

## 3. Update immutably with `Over`
`Over` applies a projection to the focused value and returns a new copy of the root object with the change applied.

```csharp
public static Contact MoveCustomer(Contact contact, string newCity)
    => ContactCity.Over(contact, _ => newCity.ToUpperInvariant());
```
_Snippet: `OpticsGuide.cs#region optics_over`_

This keeps business logic declarative—no `with` chains or manual copies of the outer objects.

## 4. Generate meaningful diagnostics
When you need to log or render the path of a failing field, call `Describe()` on the lens:

```csharp
public static string DescribeCityLens()
    => ContactCity.Describe();
```
_Snippet: `OpticsGuide.cs#region optics_describe`_

The output is something like `Contact.Address.Geo.City`, perfect for telemetry or i18n error keys.

## 5. Reuse lenses inside validators
The validation DSL integrates with lenses so you can point rules at deeply nested fields without duplicating selectors.

```csharp
public static Lens<Contact, string> ContactEmail { get; } =
    Lens.From<Contact, string>(c => c.Email, (contact, email) => contact with { Email = email });

public static Validator<Contact> ContactValidator { get; } = Validator<Contact>.Empty
    .Ensure(ContactCity, city => !string.IsNullOrWhiteSpace(city), "City is required.")
    .Ensure(ContactEmail, email => email.Contains('@'), "Email must contain '@'.");
```
_Snippet: `OpticsGuide.cs#region optics_validate`_

When `Ensure` fails, the error messages are automatically prefixed with the lens path, so the UI knows exactly which field needs attention.

## 6. Tips
- Compose once, reuse everywhere—keep your lenses in a dedicated static class.
- Pair `Lens.Identity<T>` with `Compose` when you want to swap entire aggregates while still logging the path (`$`).
- For collections, author helper lenses that locate items by ID, then compose again down to the property you need.
- Update the snippets and run `dotnet build docs/snippets/FunctionalExtensions.Snippets.csproj` whenever you evolve your optics so the docs reflect working code.
