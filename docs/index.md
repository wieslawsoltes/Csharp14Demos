# FunctionalExtensions Documentation

FunctionalExtensions brings Haskell-inspired Option, Result, Validation, and effect systems to modern C# 14 using extension members and computation expressions. These docs guide you from the first install through advanced composition patterns shipped in this repository.

## Why FunctionalExtensions
- **C# 14 first**: Exercises extension members, partial activators, and other preview capabilities so you can see the compiler features in real projects.
- **Type-class powered**: Functor, Applicative, and Monad abstractions are implemented once and shared across Option, Task, Enumerable, and other modules.
- **End-to-end samples**: The Functional CRM sample demonstrates persistence, background effects, undo stacks, and UI layers built entirely with the library.
- **Composable effects**: Readers, Writers, State, Continuations, and Task modules provide a consistent way to thread context, logs, and asynchronous work.

## Audience
| Persona | What you will find here |
| --- | --- |
| Application developers | Quickstart guides, task-based recipes, troubleshooting tips |
| Library developers & contributors | Architecture notes, type-class contracts, module reference |
| Architects & leads | Evaluation criteria, roadmap visibility, integration guidance |

## Module Snapshot
| Area | Modules | Highlights |
| --- | --- | --- |
| Core primitives | `Core`, `Unit`, `TypeClasses` | Foundational types plus shared Functor/Applicative/Monad implementations |
| Data containers | `Option`, `Result`, `Validation`, `Try` | Deterministic error handling, composition utilities, null/exception interop |
| Effects | `TaskIO`, `TaskResult`, `IO`, `Effects` | Bridge synchronous and asynchronous workflows, cancellation-aware |
| State & context | `Reader`, `Writer`, `State`, `Continuation`, `Computation` | Thread configuration, audit trails, undo stacks |
| Collections & numerics | `Sequence`, `Numerics` | Enumerable combinators, arithmetic monoids, LINQ interop |
| Optics & patterns | `Optics`, `Patterns` | Lenses, prisms, and reusable composition blueprints |

## Repository Layout
- `FunctionalExtensions/` — Core source modules described above.
- `FunctionalExtensions.Tests/` — Executable specifications and snippet backings (coming alongside future milestones).
- `FunctionalExtensions.CrmSample/` — Avalonia desktop CRM demonstrating real-world adoption.
- `Csharp14FeatureSamples/` — Console showcase of the language features that enable the library.

## Documentation Roadmap
This site follows the multi-track plan defined in `docs/functionalextensions-docs-plan.md`. Milestone 1 delivers the foundation content you are reading now. Upcoming milestones will add:
- Getting started walkthroughs (`docs/getting-started/`).
- Conceptual guides per abstraction (`docs/concepts/`).
- How-to recipes and integration samples (`docs/how-to/`, `docs/samples/`).
- Generated API reference and contribution handbook.

## Where to Next
- [Installation](./installation.md) — add the package to your solution or build from source.
- [FAQ](./faq.md) — quick answers to the most common questions.
- CRM walkthrough and quickstart guides are in progress; track issues on GitHub for updates.

## Support & Feedback
- Open documentation and feature requests on the repository issue tracker.
- Tag issues with `docs` to prioritize guidance gaps.
- Join release discussions to follow roadmap updates and preview drops.
