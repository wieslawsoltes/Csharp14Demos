# Benchmarking FunctionalExtensions

Milestone 5 introduces a repeatable BenchmarkDotNet harness so you can measure FunctionalExtensions primitives against imperative baselines. This document explains how to set up the benchmark project, what scenarios to cover, and how to interpret the results.

## 1. Project Layout
Create a new console project beside the other samples:
```bash
dotnet new console -n FunctionalExtensions.Benchmarks
dotnet add FunctionalExtensions.Benchmarks reference FunctionalExtensions/FunctionalExtensions.csproj
dotnet add FunctionalExtensions.Benchmarks package BenchmarkDotNet
```

Recommended folder structure:
```
FunctionalExtensions.Benchmarks/
  Benchmarks.csproj
  Program.cs
  OptionBenchmarks.cs
  ResultBenchmarks.cs
  ValidationBenchmarks.cs
  TaskResultBenchmarks.cs
```

## 2. Baseline Scenarios
| Area | Functional pattern | Baseline |
| --- | --- | --- |
| Option | `Option<T>.Bind/Map` chain vs `null` checks | Imperative `if (value is null)` ladder |
| Result | `Result<T>.Bind` with `TaskResult` vs exceptions | `try/catch` returning tuples |
| Validation | `Validation<T>` applicative vs sequential `if` tests | `List<string>` plus early exit |
| TaskResult pipeline | `TaskResultDoBuilder` orchestrating async IO | `Task` + exceptions + manual error propagation |

For each scenario, benchmark both the success and failure path to highlight short-circuit behavior.

## 3. Sample Benchmark (Option)
```csharp
[MemoryDiagnoser]
public class OptionBenchmarks
{
    private static readonly IDictionary<Guid, Customer> _customers = SeedCustomers();
    private static readonly IDictionary<string, string> _domains = SeedDomains();
    private readonly Guid _target = _customers.Keys.First();

    [Benchmark(Baseline = true)]
    public string Imperative()
    {
        if (!_customers.TryGetValue(_target, out var customer))
        {
            return string.Empty;
        }

        if (!customer.MarketingOptIn || !_domains.TryGetValue(customer.PreferredDomain, out var domain))
        {
            return string.Empty;
        }

        return $"{customer.Name}@{domain}";
    }

    [Benchmark]
    public string Functional()
        => Option<Customer>.Some(_customers[_target])
            .Where(c => c.MarketingOptIn)
            .Bind(c => _domains.TryGetValue(c.PreferredDomain, out var domain)
                ? Option<string>.Some($"{c.Name}@{domain}")
                : Option<string>.None)
            .ValueOr(string.Empty);
}
```

## 4. Running Benchmarks
```bash
dotnet run -c Release --project FunctionalExtensions.Benchmarks/FunctionalExtensions.Benchmarks.csproj
```

BenchmarkDotNet will emit:
- Summary table under `BenchmarkDotNet.Artifacts/results/*-report-github.md`.
- Raw data in CSV/JSON for dashboards.
- GitHub-ready markdown that can be embedded into release notes.

## 5. Publishing Results
1. Capture the GitHub-markdown report and copy it into `docs/reference/benchmarks.md` under a dated heading.
2. Summarize the key takeaways (e.g., “Functional pipeline adds ~6ns vs imperative baseline while improving clarity”).
3. Commit the updated markdown alongside any benchmark code changes.

### Example Summary
| Scenario | Baseline (ns) | Functional (ns) | Delta |
| --- | --- | --- | --- |
| Option pipeline (success) | 52.1 | 58.4 | +12% |
| Option pipeline (failure) | 19.7 | 21.1 | +7% |
| Result+TaskResult IO | 1450 | 1523 | +5% |
| Validation (5 rules) | 320 | 338 | +6% |

*(Numbers are illustrative; run the harness to generate actual values.)*

## 6. Automation Hooks
- Add a GitHub Action (future work) that runs `dotnet run -c Release --project FunctionalExtensions.Benchmarks/FunctionalExtensions.Benchmarks.csproj` on nightly builds and uploads the markdown report as an artifact.
- Gate merges that change the FunctionalExtensions library by diffing the benchmark summary.

## 7. Tips
- Warm up caches by seeding data in `[GlobalSetup]`.
- Use `[Params]` to sweep different list sizes or failure rates.
- Keep the benchmark project referencing the same source as the package so every PR benefits from automatic performance feedback.

Once the DocFX site is configured (see `docs/docfx/README.md`), include the benchmark summary in the published `reference/benchmarks` section so downstream users can evaluate trade-offs quickly.
