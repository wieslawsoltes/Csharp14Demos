# Publishing the Documentation Site

Milestone 6 ends with a public DocFX site that bundles conceptual guides, how-to recipes, samples, benchmarks, and API reference pages. This document captures the end-to-end release process.

## 1. Preconditions
- `FunctionalExtensions/FunctionalExtensions.csproj` builds with `<GenerateDocumentationFile>true</GenerateDocumentationFile>`.
- All Markdown articles pass the editorial checklist (`docs/reference/editorial-checklist.md`).
- New snippets compile: `dotnet build docs/snippets/FunctionalExtensions.Snippets.csproj`.
- Benchmarks (if updated) have their latest results embedded in `docs/reference/benchmarks.md`.

## 2. Manual Build (local validation)
```bash
dotnet build FunctionalExtensions/FunctionalExtensions.csproj
dotnet build docs/snippets/FunctionalExtensions.Snippets.csproj
cd docs/docfx
docfx metadata
docfx build
docfx serve _site
```

Verify:
- `_site/index.html` loads and links to all sections.
- API page (`_site/api/index.html`) contains the latest types and extension members.
- Mermaid diagrams render; code blocks keep syntax highlighting.

## 3. GitHub Actions Pipeline (template)
```yaml
name: docs
on:
  push:
    branches: [ main ]
  workflow_dispatch:

jobs:
  build-docs:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 10.0.x
      - name: Install docfx
        run: dotnet tool install -g docfx
      - name: Validate snippets
        run: dotnet build docs/snippets/FunctionalExtensions.Snippets.csproj
      - name: Build library (XML docs)
        run: dotnet build FunctionalExtensions/FunctionalExtensions.csproj
      - name: Build site
        working-directory: docs/docfx
        run: |
          docfx metadata
          docfx build
      - name: Upload static site
        uses: actions/upload-pages-artifact@v2
        with:
          path: docs/docfx/_site
  deploy:
    needs: build-docs
    permissions:
      pages: write
      id-token: write
    environment:
      name: github-pages
      url: ${{ steps.deployment.outputs.page_url }}
    runs-on: ubuntu-latest
    steps:
      - uses: actions/deploy-pages@v2
```

Update the workflow file in `.github/workflows/docs.yml` (future work) using the snippet above.

## 4. Versioning
- Tag releases in git (`v0.1.0`, etc.) and include a link to the corresponding DocFX site snapshot (e.g., `https://<org>.github.io/Csharp14Demos/v0.1.0/` if you publish per version).
- Maintain a `latest` site for main branch, plus optional release-specific builds.
- Store benchmark reports and changelog entries per release so visitors can correlate documentation with NuGet packages.

## 5. Post-Publish Checklist
- [ ] Smoke test the deployed site on GitHub Pages (or chosen host).
- [ ] Validate API search/autocomplete.
- [ ] Ensure new locales (when available) appear in the language selector.
- [ ] Update README badges to include the documentation status (e.g., “Docs: Published” with link).

## 6. Rollback Plan
- Keep previous `_site` artifacts (via GitHub Pages history or static backups) so you can redeploy if a build breaks.
- Use `git revert` on doc changes if necessary; rerun the workflow to republish.

Having this process documented allows contributors to ship changes confidently and ensures the DocFX pipeline remains reproducible on any machine or CI service.
