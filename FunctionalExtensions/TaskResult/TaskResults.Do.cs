using FunctionalExtensions.Computation;

namespace FunctionalExtensions;

/// <summary>
/// Computation-expression entry points for <see cref="TaskResult{T}"/>.
/// </summary>
public static partial class TaskResults
{
    /// <summary>
    /// Provides a do-notation builder that supports fluent monadic workflows.
    /// </summary>
    public static TaskResultDoBuilder Do => new();
}
