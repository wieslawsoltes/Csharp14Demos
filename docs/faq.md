# FunctionalExtensions FAQ

### What problem does FunctionalExtensions solve?
It delivers a consistent set of functional primitives (Option, Result, Validation, TaskResult, etc.) that embrace C# 14 extension members so you can build declarative, composable workflows without reinventing glue code across projects.

### Why use this instead of LanguageExt or other libraries?
FunctionalExtensions focuses on the latest compiler features, keeps the API surface small, and aligns directly with the CRM sample and tests in this repository. If you need broad ecosystem integrations, LanguageExt may still fit, but this library optimizes for modern syntax and teaching scenarios.

### Which platforms are supported?
Target framework is `net10.0`, which currently runs on Windows, macOS, and Linux with the .NET 10 preview SDK. The abstractions themselves are portable, so once .NET 10 is GA the package will support any runtime implementing the target framework.

### Do I need to enable preview features?
Yes. C# 14 syntax and extension members require `<LangVersion>14.0</LangVersion>` and `<EnablePreviewFeatures>true</EnablePreviewFeatures>`. Without those flags you will see compiler errors such as CS8652.

### Can I use this in production?
The codebase is production-grade, but because it depends on preview SDKs you should validate with your organization’s risk guidelines. We recommend locking the SDK via `global.json`, running the unit tests, and monitoring release notes for breaking changes while the version remains `0.x`.

### How do I learn the library quickly?
Start with the [Installation](./installation.md) guide, then work through the upcoming quickstart in `docs/getting-started/quickstart.md` (arriving in Milestone 2). The CRM sample under `FunctionalExtensions.CrmSample/` demonstrates integration across storage, networking, and UI.

### Where are the API docs?
XML documentation is being enabled during Milestone 2, after which a DocFX-generated API site will live under `docs/reference/`. Until then, browse the `FunctionalExtensions` folder directly or inspect IntelliSense summaries when referencing the package.

### What is the roadmap?
See `docs/functionalextensions-docs-plan.md` for the multi-milestone documentation plan. Release tracking and feature planning happen in GitHub issues tagged `roadmap`.

### How do I report bugs or request new modules?
Open an issue in the repository with a clear title, reproduction steps, and environment details. Tag it with `bug`, `docs`, or `enhancement` so it routes to the right maintainer. Security-sensitive reports should be emailed directly to the maintainer rather than posted publicly.

### Can I contribute documentation?
Yes. Follow the CONTRIBUTING instructions (to be published alongside the contribution guide) and submit a pull request. Documentation changes should run markdown linting (`markdownlint-cli2`) and link checks inside the `docs` folder before requesting review.
