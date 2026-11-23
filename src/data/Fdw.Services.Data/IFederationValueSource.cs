using Fdw.Collections;

namespace Fdw.Services.Data;

/// <summary>Where a resolved federation value came from (FDW-6901).</summary>
public interface IFederationValueSource : ITypeOption<int, FederationValueSourceBase>
{
}
