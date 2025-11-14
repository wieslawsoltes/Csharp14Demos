# Localization & Terminology Strategy

Milestone 6 introduces a formal process for translating the FunctionalExtensions docs and aligning terminology across languages. Use this guidance when planning community translations or vetting contributions.

## 1. Prioritized Content
| Tier | Content | Rationale |
| --- | --- | --- |
| Tier 0 | Landing page (`docs/index.md`), installation, quickstart, CRM walkthrough | Highest traffic; critical for adoption |
| Tier 1 | Concepts (Option/Result/Validation), how-to guides, FAQ | Core learning path |
| Tier 2 | Reference materials (benchmarks, DocFX README, contribution guide) | Useful but optional for first pass |

Focus on Tier 0/1 for the initial localization sprint. Tier 2 can follow as volunteer bandwidth allows.

## 2. Terminology Glossary
Create a CSV or YAML glossary under `docs/reference/glossary.*` (future work). Seed it with the following English terms and approved translations:

| Term | Definition | Notes |
| --- | --- | --- |
| Option | Discriminated union representing “some” or “none” | Keep as borrowed term if target language lacks FP jargon |
| Result | Success/failure wrapper | Align with .NET docs (“Result” vs “Outcome”) |
| TaskResult | Async Result wrapper | Do not translate `Task` |
| Lens | Functional optic | Provide short description (“focus handle”) |
| Validator | Rule set builder | Map to existing industry terminology |

Translators should reference this glossary before contributing. If a term lacks consensus, log an issue and discuss with maintainers.

## 3. Style Guidelines for Translations
- Maintain code blocks, commands, file paths, and API names in English.
- Translate prose, table headers, and captions.
- Keep headings aligned with the English structure so DocFX can map localized TOCs.
- Avoid sarcasm or idiomatic expressions that do not transfer well.
- Use second person (“You…”) and imperative instructions.

## 4. Workflow
1. Fork or branch per language (e.g., `docs/<lang>/index.md`). DocFX supports locale folders; we will add TOC wiring once translations exist.
2. Run the snippet build to ensure code still compiles.
3. Add translators to the PR as reviewers; at least one native speaker must sign off.
4. Update the localization status table below.

### Status Tracker (sample)
| Locale | Tier 0 | Tier 1 | Tier 2 | Owner |
| --- | --- | --- | --- | --- |
| `en-us` | ✅ | ✅ | ✅ | Core team |
| `pl-pl` | 🚧 | ⬜ | ⬜ | TBD |
| `es-es` | ⬜ | ⬜ | ⬜ | TBD |

Legend: ✅ complete, 🚧 in progress, ⬜ not started.

## 5. Tooling
- Use `markdownlint` with language-aware rules (set `defaultLanguage: "<locale>"` per folder).
- Optional: integrate with Crowdin or GitLocalize if the community grows; this doc will be updated with machine-friendly workflows as needed.

## 6. Future Enhancements
- Add language selector in the DocFX site once at least one non-English locale ships.
- Mirror glossary entries in the codebase (XML docs) so IDE tooltips remain consistent.
- Provide localized screenshots for the CRM sample (ensure text is externalized or annotated).
