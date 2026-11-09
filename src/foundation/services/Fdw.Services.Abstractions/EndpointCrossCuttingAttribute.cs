using System;

namespace Fdw.Services.Abstractions;

/// <summary>
/// Marks a cross-cutting type an endpoint may take through its constructor beside its domain's provider
/// interface (FDW060, FDW-4618 rule 1): audit, access policy, tenant context, authorization.
/// </summary>
/// <remarks>
/// Which types get this mark is Mike's ruling (FDW-4618, "Needs a ruling from Mike"). Until then no type
/// carries it, and the types the sweep allows are named in the <c>fdw_endpoint_cross_cutting_types</c>
/// .editorconfig key, which FDW060 reads.
/// </remarks>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class EndpointCrossCuttingAttribute : Attribute
{
}
