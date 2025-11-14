# DocFX Site Scaffold

Milestone 5 introduces DocFX so the conceptual docs (`docs/**/*.md`) and API reference share a single published site.

## Installing DocFX
```bash
dotnet tool install -g docfx
```
or download the latest release from <https://github.com/dotnet/docfx>.

## Generating Metadata & Site
```bash
cd docs/docfx
docfx metadata
docfx build
```

- `metadata` scans `FunctionalExtensions/FunctionalExtensions.csproj`, reads the XML documentation file emitted during build, and writes YAML under `docs/docfx/api/`.
- `build` combines the generated YAML with the Markdown content referenced in `docfx.json` and emits a static site at `docs/docfx/_site/`.

## Publishing
- Use `docfx serve _site` locally for spot checks.
- In CI, add a workflow that runs `docfx metadata && docfx build` and publishes `_site` to GitHub Pages (or as an artifact).
- The DocFX pipeline should run after `dotnet build` so `FunctionalExtensions.xml` is up-to-date.

## File Map
| Path | Purpose |
| --- | --- |
| `docfx.json` | DocFX configuration (metadata sources, content globbing, site metadata). |
| `api/` | Generated YAML + `index.md` placeholder. Do not edit the YAML files manually. |
| `_site/` | Build output. Add to `.gitignore` if not already excluded. |

## Troubleshooting
- **Missing XML documentation**: ensure `FunctionalExtensions.csproj` has `<GenerateDocumentationFile>true</GenerateDocumentationFile>`. Run `dotnet build` before `docfx metadata`.
- **File glob misses new docs**: update the `build.content` section in `docfx.json`.
- **Preview features errors**: install .NET 10 preview SDK—the same requirement as the rest of the repo.
