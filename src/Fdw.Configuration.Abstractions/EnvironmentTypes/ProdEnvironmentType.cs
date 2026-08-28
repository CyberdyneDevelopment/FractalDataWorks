using Fdw.Collections.Attributes;

namespace Fdw.Configuration;

/// <summary>
/// Production environment — live system, real data, full security.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(EnvironmentTypes), "Prod")]
public sealed class ProdEnvironmentType : EnvironmentTypeBase
{
    /// <summary>
    /// Initializes a new instance of <see cref="ProdEnvironmentType"/>.
    /// </summary>
    public ProdEnvironmentType() : base(4, "Prod")
    {
    }
}
