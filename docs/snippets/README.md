# Documentation Snippets

The `FunctionalExtensions.Snippets.csproj` project compiles every code sample published in the docs. Keep snippets and markdown synchronized so changes stay verifiable.

## Folder Layout
- `Quickstart/MinimalApiQuickstart.cs` – backing code for `docs/getting-started/quickstart.md`.
- `Concepts/OptionConcepts.cs`, `ResultConcepts.cs`, `ValidationConcepts.cs` – examples referenced from `docs/concepts/*.md`.
- `HowTo/AsyncPipelines.cs`, `StatefulWorkflows.cs`, `OpticsGuide.cs` – runnable snippets for `docs/how-to/*.md`.
- Future milestones will add folders for conceptual guides, how-to recipes, and CRM walkthroughs.

## Running the Snippets
```bash
dotnet build docs/snippets/FunctionalExtensions.Snippets.csproj
```

The build fails if a snippet diverges from the current API surface, making it easy to catch breaking changes before publishing docs or packages.

Add new snippets by:
1. Creating a `.cs` file in `docs/snippets/<topic>/`.
2. Wrapping each doc-exported section with a named `#region`.
3. Referencing the region in the corresponding markdown guide (copy/paste or future tooling).
