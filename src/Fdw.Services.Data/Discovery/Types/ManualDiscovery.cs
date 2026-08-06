using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Microsoft.Extensions.Configuration;

namespace Fdw.Services.Data.Discovery.Types;

/// <summary>
/// Manual container discovery -- containers are defined explicitly by the user.
///
/// Identity properties (readonly, set by constructor):
///   Id=3, Name="Manual", DisplayName="Manual Discovery"
///
/// Configuration properties (bindable from IConfiguration):
///   None -- manual discovery has no configuration; containers are defined individually.
///
/// Behavior:
///   SupportsAutoDiscovery=false -- containers must be defined by the user.
/// </summary>
[TypeOption(typeof(DiscoveryMethods), "Manual")]
public sealed class ManualDiscovery : DiscoveryMethodBase
{
    private static readonly string[] Expected = [];
    private static readonly string[] Required = [];

    /// <summary>Initializes a new instance of the <see cref="ManualDiscovery"/> class.</summary>
    public ManualDiscovery()
        : base(
            id: 3,
            name: "Manual",
            displayName: "Manual Discovery",
            description: "Containers are defined explicitly by the user",
            supportsAutoDiscovery: false,
            expectedProperties: Expected,
            requiredProperties: Required)
    {
    }

    /// <inheritdoc/>
    public override DiscoveryMethodBase CreateInstance() => new ManualDiscovery();

    /// <inheritdoc/>
    public override IGenericResult Validate()
    {
        return GenericResult.Success();
    }
}
