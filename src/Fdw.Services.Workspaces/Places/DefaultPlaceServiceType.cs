using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Fdw.Services.Workspaces;

/// <summary>
/// The default place service type: one source for each kind of place, read from the configuration that holds places of that kind, and the service
/// that follows them up the tree (FDW-2538).
/// </summary>
/// <remarks>
/// A new level of the tree is a new <see cref="IPlaceParentSource"/> registered by the service type that owns that level. The sources are added with
/// TryAddEnumerable, so a source another package registered is kept and the same source is never added twice.
/// </remarks>
[ExcludeFromCodeCoverage]
[Implementation(typeof(PlaceServiceTypes), "Default")]
public sealed class DefaultPlaceServiceType : PlaceServiceTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultPlaceServiceType"/> class.
    /// </summary>
    public DefaultPlaceServiceType()
        : base(
            "Default",
            "Places:Default",
            "Default Place Services",
            "The scope tree of tenant, organization and workspace, and the places above a place")
    {
        Registration((builder, _) =>
        {
            builder.Services.TryAddEnumerable(ServiceDescriptor.Scoped<IPlaceParentSource, TenantPlaceParentSource>());
            builder.Services.TryAddEnumerable(ServiceDescriptor.Scoped<IPlaceParentSource, OrganizationPlaceParentSource>());
            builder.Services.TryAddEnumerable(ServiceDescriptor.Scoped<IPlaceParentSource, WorkspacePlaceParentSource>());
            builder.Services.TryAddScoped<IPlaceAncestry, PlaceAncestry>();
            builder.Services.TryAddScoped<IPlaceNames, PlaceNames>();
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
    }
}
