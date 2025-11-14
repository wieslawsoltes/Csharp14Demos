# Installing FunctionalExtensions

FunctionalExtensions targets .NET 10 with the C# 14 language preview. This guide walks you through installing the NuGet package, building from source, and configuring your projects to use the new language features safely.

## Prerequisites
- .NET SDK 10.0 preview (matching the `global.json` in the repo).
- C# 14 language support enabled via `<LangVersion>14.0</LangVersion>` in your project.
- IDE with preview feature support (Visual Studio 2022 Preview, JetBrains Rider EAP, or VS Code with latest C# Dev Kit).
- Optional: Avalonia tooling if you plan to run the CRM sample.

Verify your environment:
```bash
dotnet --info
```
Confirm the SDK version matches the repo requirement.

## Install from NuGet
FunctionalExtensions ships as the `FunctionalExtensions.TypeClasses` package. Until the first stable release is published, install the prerelease build:
```bash
dotnet add package FunctionalExtensions.TypeClasses --version 0.1.0-preview
```

For Paket:
```bash
paket add FunctionalExtensions.TypeClasses --version 0.1.0-preview
```

## Install from Source
Use this flow when testing unreleased changes or contributing.
```bash
git clone https://github.com/wieslawsoltes/Csharp14Demos.git
cd Csharp14Demos/FunctionalExtensions
dotnet restore
dotnet pack -c Release
```

The `dotnet pack` command produces a `.nupkg` under `FunctionalExtensions/bin/Release`. Add it to your local feed:
```bash
dotnet nuget add source "$(pwd)/bin/Release" --name FunctionalExtensionsLocal
```
Then install as usual:
```bash
dotnet add package FunctionalExtensions.TypeClasses --source FunctionalExtensionsLocal
```

## Enable C# 14 Preview Features
Add or confirm these settings in your consuming project file:
```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
  <LangVersion>14.0</LangVersion>
  <EnablePreviewFeatures>true</EnablePreviewFeatures>
</PropertyGroup>
```
If you are multi-targeting, set the properties on the `net10.0` target only.

## Verifying the Installation
1. Restore packages: `dotnet restore`.
2. Compile your project: `dotnet build`.
3. Add a smoke test using the Option module:
   ```csharp
   using FunctionalExtensions;

   var maybeUser = Option<string>.Some("Codex");
   Console.WriteLine(maybeUser.HasValue);
   ```
4. Run the CRM sample to validate Avalonia dependencies:
   ```bash
   dotnet run --project FunctionalExtensions.CrmSample/FunctionalExtensions.CrmSample.csproj
   ```

## Upgrading
- Follow semantic versioning: `0.x` indicates breaking changes may occur between releases.
- Review release notes in the root `CHANGELOG` (to be added) or the GitHub Releases feed.
- Rebuild locally when updating preview SDKs; new language features may require IDE updates.

## Troubleshooting
- **CS8652 preview errors**: Ensure `EnablePreviewFeatures` is set true and your IDE uses the same SDK version as the CLI.
- **NuGet restore fails**: Clear caches (`dotnet nuget locals all --clear`) and confirm the local feed path when consuming nightly builds.
- **Avalonia runtime issues**: Install platform prerequisites (Linux: `libgtk-3-0`, macOS: .NET MAUI workloads not required).
- **Missing XML docs**: Build the library with `dotnet build /p:GenerateDocumentationFile=true` to include API comments in IntelliSense.
