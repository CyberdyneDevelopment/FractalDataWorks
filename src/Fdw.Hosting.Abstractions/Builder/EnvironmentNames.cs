namespace Fdw.Hosting.Abstractions.Builder;

/// <summary>
/// Well-known environment names.
/// </summary>
// Why: pure data holder, no logic beyond trivial construction/assignment
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public static class EnvironmentNames
{
    /// <summary>
    /// Development environment.
    /// </summary>
    public const string Development = "Development";

    /// <summary>
    /// Staging environment.
    /// </summary>
    public const string Staging = "Staging";

    /// <summary>
    /// Production environment.
    /// </summary>
    public const string Production = "Production";

    /// <summary>
    /// Test environment.
    /// </summary>
    public const string Test = "Test";
}
