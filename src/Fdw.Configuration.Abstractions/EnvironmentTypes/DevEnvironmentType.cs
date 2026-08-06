using Fdw.Collections.Attributes;

namespace Fdw.Configuration;

/// <summary>
/// Shared development environment — team integration, shared services.
/// </summary>
// Why: data-bearing TypeOption; ctor only forwards literal/config data to the base class, no behavior
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(EnvironmentTypes), "Dev")]
public sealed class DevEnvironmentType : EnvironmentTypeBase
{
    /// <summary>
    /// Initializes a new instance of <see cref="DevEnvironmentType"/>.
    /// </summary>
    public DevEnvironmentType() : base(2, "Dev")
    {
    }
}
