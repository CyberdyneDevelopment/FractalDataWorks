using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// The feature catalogue (FDW-1580): every feature the system knows, each switched on for a tenant by the platform and then for the
/// tenant's organizations by the tenant.
/// </summary>
[TypeCollection(typeof(FeatureBase), typeof(IFeature), typeof(Features))]
[ExcludeFromCodeCoverage]
public abstract partial class Features : TypeCollectionBase<FeatureBase, IFeature>
{
}
