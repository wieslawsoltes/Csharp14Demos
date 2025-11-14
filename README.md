# C# 14 Feature Samples

This console application demonstrates the headline features introduced with C# 14 and .NET 10.  
Each sample is isolated in its own `IFeatureDemo` implementation under `Features/` and is wired up by `Program.cs` so you can see the feature in action alongside explanatory console output.

## Prerequisites

- .NET SDK 10.0 or newer

The project pins `LangVersion` to 14.0 in the `.csproj`, so the compiler enables the preview syntax required for these scenarios.

## Running the samples

```bash
dotnet run --project Csharp14FeatureSamples/Csharp14FeatureSamples.csproj
```

## Feature index

| Feature | Demo file | Documentation |
| --- | --- | --- |
| Extension members | `Features/ExtensionMembersDemo.cs` | [What's new in C# 14 – Extension members](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14#extension-members) |
| `field` backed properties | `Features/FieldKeywordDemo.cs` | [What's new in C# 14 – The field keyword](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14#the-field-keyword) |
| Implicit span conversions | `Features/ImplicitSpanConversionsDemo.cs` | [What's new in C# 14 – Implicit span conversions](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14#implicit-span-conversions) |
| `nameof` with unbound generics | `Features/NameOfUnboundGenericDemo.cs` | [What's new in C# 14 – Unbound generic types and nameof](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14#unbound-generic-types-and-nameof) |
| Lambda parameter modifiers | `Features/SimpleLambdaModifiersDemo.cs` | [What's new in C# 14 – Simple lambda parameters with modifiers](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14#simple-lambda-parameters-with-modifiers) |
| Partial constructors and events | `Features/PartialMembersDemo.cs` | [What's new in C# 14 – More partial members](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14#more-partial-members) |
| User-defined compound assignment | `Features/UserDefinedCompoundAssignmentDemo.cs` | [What's new in C# 14 – User defined compound assignment](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14#user-defined-compound-assignment) |
| Null-conditional assignment | `Features/NullConditionalAssignmentDemo.cs` | [What's new in C# 14 – Null-conditional assignment](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14#null-conditional-assignment) |

## Repository layout

- `Csharp14FeatureSamples.csproj` – Project file pinned to .NET 10 with `LangVersion` 14.0.
- `Program.cs` – Entry point that enumerates and executes each `IFeatureDemo`.
- `Features/` – Folder containing individual, well-commented samples for every C# 14 feature listed above.

Feel free to open a specific demo file to copy the snippet into your own projects or to experiment further with the new syntax.

## Functional CRM sample

The repository now also includes a realistic Avalonia desktop application under `FunctionalExtensions.CrmSample/` that exercises the entire `FunctionalExtensions` library across a mini CRM workflow (SQLite persistence, file attachments, HTTP enrichment, background notifications, undo stack powered by continuations, etc.).

```bash
dotnet run --project FunctionalExtensions.CrmSample/FunctionalExtensions.CrmSample.csproj
```

> The sample uses .NET 10 preview + C# 14, so be sure to install the matching SDK referenced in `global.json`.
