using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Data.Results;
using Microsoft.Extensions.Configuration;

namespace Fdw.Services.Data.Discovery.Types;

/// <summary>
/// Automatic container discovery -- discovers the data store schema to discover containers.
///
/// Identity properties (readonly, set by constructor):
///   Id=1, Name="Auto", DisplayName="Automatic Discovery"
///
/// Configuration properties (bindable from IConfiguration):
///   SchemaFilter -- optional schema name filter for targeted discovery.
///
/// Behavior:
///   SupportsAutoDiscovery=true -- can discover schemas without user input.
/// </summary>
[TypeOption(typeof(DiscoveryMethods), "Auto")]
public sealed class AutoDiscovery : DiscoveryMethodBase
{
    private static readonly string[] Expected = ["SchemaFilter"];
    private static readonly string[] Required = [];

    /// <summary>Initializes a new instance of the <see cref="AutoDiscovery"/> class.</summary>
    public AutoDiscovery()
        : base(
            id: 1,
            name: "Auto",
            displayName: "Automatic Discovery",
            description: "Discovers the data store schema to automatically discover containers",
            supportsAutoDiscovery: true,
            expectedProperties: Expected,
            requiredProperties: Required)
    {
    }

    /// <summary>Gets or sets an optional schema name filter for targeted discovery.</summary>
    public string? SchemaFilter { get; set; }

    /// <inheritdoc/>
    public override DiscoveryMethodBase CreateInstance() => new AutoDiscovery();

    /// <inheritdoc/>
    public override void Bind(IConfigurationSection section)
    {
        SchemaFilter = section[nameof(SchemaFilter)];
    }

    /// <inheritdoc/>
    public override void BindFromValues(IReadOnlyDictionary<string, string?> values)
    {
        values.TryGetValue(nameof(SchemaFilter), out var sf);
        SchemaFilter = sf;
    }

    /// <inheritdoc/>
    public override IReadOnlyList<KeyValuePair<string, string?>> AsKvp()
    {
        return
        [
            new("Type", Name),
            new(nameof(SchemaFilter), SchemaFilter),
        ];
    }

    /// <inheritdoc/>
    public override IGenericResult Validate()
    {
        return GenericResult.Success();
    }
}
