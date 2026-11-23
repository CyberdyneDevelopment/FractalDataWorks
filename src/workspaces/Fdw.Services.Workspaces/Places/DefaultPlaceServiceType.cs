using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.ServiceTypes;

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
    // The most versions of one definition one read across places holds (FDW-7104), by the key a host states it under (Places:Default). A definition holds
    // at most one version per place. A host changes it with ServiceTypeDefaults.Override before Register.
    private static readonly IReadOnlyDictionary<string, object> Defaults = new Dictionary<string, object>(StringComparer.Ordinal)
    {
        [nameof(PlaceVersionLimits.MaxVersions)] = 1000,
    };

    /// <inheritdoc />
    public override IReadOnlyDictionary<string, object> DeclaredDefaults => Defaults;

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
        Registration((builder, loggerFactory) =>
        {
            var maxVersions = DeclaredSetting.ConfiguredCount(builder, ConfigurationKey, this, nameof(PlaceVersionLimits.MaxVersions),
                (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<DefaultPlaceServiceType>());
            if (!maxVersions.IsSuccess)
                return maxVersions.ToNewResult<IHostApplicationBuilder>();

            builder.Services.TryAddSingleton(new PlaceVersionLimits(maxVersions.Value));
            builder.Services.TryAddEnumerable(ServiceDescriptor.Scoped<IPlaceParentSource, TenantPlaceParentSource>());
            builder.Services.TryAddEnumerable(ServiceDescriptor.Scoped<IPlaceParentSource, OrganizationPlaceParentSource>());
            builder.Services.TryAddEnumerable(ServiceDescriptor.Scoped<IPlaceParentSource, WorkspacePlaceParentSource>());
            builder.Services.TryAddScoped<IPlaceAncestry, PlaceAncestry>();
            builder.Services.TryAddScoped<IPlaceNames, PlaceNames>();
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
    }
}
