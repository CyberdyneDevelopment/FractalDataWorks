using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Conventions;

/// <summary>
/// Override convention analyzer thresholds for a specific method or class.
/// Values of -1 indicate "use the default threshold".
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
[ExcludeFromCodeCoverage]
public sealed class ConventionOverrideAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the maximum number of executable lines allowed in a method (FDW006).
    /// Set to -1 to use the default threshold.
    /// </summary>
    public int MaxMethodLines { get; set; } = -1;

    /// <summary>
    /// Gets or sets the maximum cyclomatic complexity allowed in a method (FDW007).
    /// Set to -1 to use the default threshold.
    /// </summary>
    public int MaxCyclomaticComplexity { get; set; } = -1;
}
