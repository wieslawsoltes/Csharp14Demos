# Editorial Review Checklist

Milestone 6 focuses on quality assurance for the documentation set. Use this checklist before publishing or accepting PRs that modify docs.

## 1. Scope & Structure
- [ ] Topic matches the information architecture (foundation, getting-started, concepts, how-to, samples, reference).
- [ ] Title is action-oriented and unique.
- [ ] Front-matter (if added later) and headings follow sentence case.
- [ ] Sections progress from context → procedure → next steps; tables and code blocks include captions when helpful.

## 2. Technical Accuracy
- [ ] Commands run against the current repo layout (`dotnet` paths, project names, CLI flags).
- [ ] Code snippets exist in `docs/snippets/**` with matching `#region` tags.
- [ ] `dotnet build docs/snippets/FunctionalExtensions.Snippets.csproj` succeeds locally.
- [ ] References to SDK versions (C# 14, .NET 10) match `global.json`.
- [ ] API names use fully qualified paths when ambiguity is possible (`ReaderTaskResult<AppEnv, TResult>` vs `TaskResult<TResult>`).

## 3. Style & Voice
- [ ] Uses present tense and second person (“You can…”).
- [ ] Avoids passive constructions and marketing fluff; emphasize actionable guidance.
- [ ] Keeps sentences under ~25 words unless quoting code.
- [ ] Uses inline code for identifiers (`Option<T>`, `TaskResultDoBuilder`) and fenced blocks for multi-line snippets.

## 4. Links & Cross-References
- [ ] Relative links work within the DocFX site (no absolute file:// paths).
- [ ] Every new article links to related guides (e.g., how-tos referencing concepts, samples referencing how-tos).
- [ ] External links (DocFX, BenchmarkDotNet) include protocol and have been verified.

## 5. Accessibility & Formatting
- [ ] Tables have headers.
- [ ] Diagrams (Mermaid) include descriptive text before or after the block.
- [ ] Images (when added) contain alt text.
- [ ] Keyboard sequences and CLI commands render correctly in dark/light themes.

## 6. Localization Readiness
- [ ] Terminology aligns with the glossary (to be tracked in `docs/reference/localization.md`).
- [ ] Avoids idioms and culturally specific metaphors.
- [ ] Strings destined for UI (e.g., “Customer is not active.”) match the application resource files.

## 7. Automation Hooks
- Run before merge:
  ```bash
  dotnet build docs/snippets/FunctionalExtensions.Snippets.csproj
  docfx metadata
  docfx build
  ```
- Optional linting: `markdownlint-cli2 docs` and `cspell "**/*.md"` (to be wired into CI).

## 8. Sign-off
- Tech writer and SME both approve each doc PR.
- For major additions (conceptual/how-to), ensure at least one maintainer validates code samples in the actual app/sample repo.

Store this file under `docs/reference/` and link it from team onboarding materials so every release follows the same QA bar.
