using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Resiliency.Abstractions;

/// <summary>
/// General purpose simple retry for basic operations.
/// Uses minimal retry logic with short delays for quick failure recovery.
/// </summary>
[TypeOption(typeof(ResiliencyCategories), "Simple")]
[ExcludeFromCodeCoverage]
public sealed class SimpleResiliencyCategory : ResiliencyCategoryBase
{
    /// <summary>Initializes a new instance of <see cref="SimpleResiliencyCategory"/>.</summary>
    public SimpleResiliencyCategory() : base(4, "Simple") { }
}
