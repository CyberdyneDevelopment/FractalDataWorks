using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Data;

/// <summary>
/// TypeCollection for where a resolved federation value came from (FDW-6901), carried on <see cref="ResolvedFederation"/> for the read's trace:
/// the data set's own value, the FDW-level setting, or the value the strategy declares.
/// </summary>
[TypeCollection(typeof(FederationValueSourceBase), typeof(IFederationValueSource), typeof(FederationValueSources))]
[ExcludeFromCodeCoverage]
public abstract partial class FederationValueSources : TypeCollectionBase<FederationValueSourceBase, IFederationValueSource> { }
