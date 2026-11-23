using Fdw.Collections.Attributes;

namespace Fdw.Services.Data;

/// <summary>The value the strategy declares on itself.</summary>
[TypeOption(typeof(FederationValueSources), "Declared")]
public sealed class DeclaredFederationValueSource : FederationValueSourceBase
{
    /// <summary>Initializes a new instance of the <see cref="DeclaredFederationValueSource"/> class.</summary>
    public DeclaredFederationValueSource()
        : base("Declared")
    {
    }
}
