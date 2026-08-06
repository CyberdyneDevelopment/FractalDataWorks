using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Resiliency.Abstractions;

/// <summary>
/// Critical operations requiring aggressive retry behavior.
/// Used for essential operations where failure is not an acceptable outcome.
/// </summary>
[TypeOption(typeof(ResiliencyCategories), "Critical")]
[ExcludeFromCodeCoverage]
public sealed class CriticalResiliencyCategory : ResiliencyCategoryBase
{
    /// <summary>Initializes a new instance of <see cref="CriticalResiliencyCategory"/>.</summary>
    public CriticalResiliencyCategory() : base(3, "Critical") { }
}
