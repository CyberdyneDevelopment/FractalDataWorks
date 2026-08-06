using Fdw.Collections.Attributes;

namespace Fdw.Configuration;

/// <summary>
/// Local development environment — developer machine, local services.
/// </summary>
// Why: data-bearing TypeOption; ctor only forwards literal/config data to the base class, no behavior
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(EnvironmentTypes), "Local")]
public sealed class LocalEnvironmentType : EnvironmentTypeBase
{
    /// <summary>
    /// Initializes a new instance of <see cref="LocalEnvironmentType"/>.
    /// </summary>
    public LocalEnvironmentType() : base(1, "Local")
    {
    }
}
