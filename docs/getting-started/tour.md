# FunctionalExtensions Tour

This tour orients you across the major modules so you can decide which abstractions to reach for in real projects. Pair it with the [Quickstart](./quickstart.md) to see the pieces working together.

## Core Building Blocks
- **Unit (`FunctionalExtensions/Core/Unit.cs`)** – a lightweight value representing “no result”, used across IO/effect modules.
- **Option (`FunctionalExtensions/Option/`)** – `Option<T>` plus helper factories (`Option.Some`, `Option.None`, `Option.FromNullable`). Use it when a missing value is not an error.
- **Result (`FunctionalExtensions/Result/`)** – `Result<T>` encapsulates success or failure with an error message. `Result.Try` captures exceptions and turns them into explicit failures.
- **Validation (`FunctionalExtensions/Validation/`)** – applicative validation that accumulates multiple errors via `Validation<T>` and the fluent `Validator<T>` DSL (`FunctionalExtensions/Validation/Validator.cs`).

## Type Classes & Extension Members
`FunctionalExtensions/TypeClasses/` houses the reusable Functor/Applicative/Monad behaviors for common types:
- `OptionMonad`, `TaskMonad`, and `EnumerableMonad` declare extension members using C# 14’s `extension` syntax, enabling query expressions and LINQ-style comprehension.
- Applicative modules expose `Return`, `Apply`, and `Map` so you can compose independent computations without manual plumbing.
- These types are internalized in the NuGet package, meaning downstream users get the ergonomic APIs without re-declaring helpers themselves.

## Async, IO, and Effects
- **TaskResult (`FunctionalExtensions/TaskResult/`)** wraps asynchronous operations that produce `Result<T>` values. Use it to ensure async flows still surface success/failure explicitly.
- **TaskIO / IO (`FunctionalExtensions/TaskIO/`, `FunctionalExtensions/IO/`)** provide safe wrappers around effectful operations, exposing helpers like `TaskIO.ToTaskResult`.
- **Effects (`FunctionalExtensions/Effects/`)** contains primitives for channels, timers, and background pipelines that return `TaskResult<Unit>` so failures stay observable.
- **Computation Builders (`FunctionalExtensions/Computation/TaskResultDoBuilder.cs`)** let you write do-notation style workflows that feel like idiomatic C# but keep the functional semantics.

## Stateful Composition
- **Reader / Writer / State (`FunctionalExtensions/Reader/`, `FunctionalExtensions/Writer/`, `FunctionalExtensions/State/`)** offer simple monads for threading configuration, logging, and mutable state through pure functions.
- **Continuation and Computation** modules show how to defer work or build undo stacks, as showcased in the CRM sample.
- **Sequence & Numerics** supply LINQ-friendly combinators and monoidal math helpers useful when processing event streams or aggregations.

## Optics & Patterns
- **Optics (`FunctionalExtensions/Optics/`)** implements lenses/prisms using extension members so you can focus into nested structures without reflection.
- **Patterns (`FunctionalExtensions/Patterns/`)** captures real-world composition recipes—error pipelines, transactional workflows, etc.—that you can adapt to your domain.

## Samples & Tests
- **CRM Sample (`FunctionalExtensions.CrmSample/`)** – Avalonia desktop app wiring together Option, Result, Validation, TaskIO, and stateful effects (undo stack, notifications, persistence).
- **Tests (`FunctionalExtensions.Tests/`)** – houses executable specs and will also host snippet verifications alongside Milestone 3+.

## Next Steps
1. Follow the [Quickstart guide](./quickstart.md) to compose Option/Result pipelines inside a minimal API.
2. Explore individual module folders listed above and read the XML doc comments (enable them with `/p:GenerateDocumentationFile=true`) for API-level details.
3. Track the upcoming conceptual deep dives (`docs/concepts/`) if you need a slower introduction to monads, optics, or continuation-passing style.
