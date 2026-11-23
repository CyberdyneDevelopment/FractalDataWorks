using Fdw.Collections;

namespace Fdw.Services.Data;

/// <summary>The base of each place a resolved federation value can come from (FDW-6901).</summary>
public abstract class FederationValueSourceBase : TypeOptionBase<FederationValueSourceBase>, IFederationValueSource
{
    /// <summary>Initializes a new instance of the <see cref="FederationValueSourceBase"/> class.</summary>
    /// <param name="name">The source's name, as the trace shows it.</param>
    protected FederationValueSourceBase(string name)
        : base(name)
    {
    }
}
