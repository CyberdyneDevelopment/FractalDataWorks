using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Results;
using Microsoft.Extensions.Configuration;

namespace Fdw.Services.Data.Discovery;

/// <summary>
/// Base class for container discovery methods.
/// Each concrete type (Auto, FromFile, Manual) extends this with its own
/// bindable properties and behavior.
///
/// The TypeCollection holds one singleton prototype per type. Call CreateInstance()
/// on the prototype to get a fresh instance whose { get; set; } properties can be
/// bound from IConfiguration.
/// </summary>
public abstract class DiscoveryMethodBase
    : TypeOptionBase<int, DiscoveryMethodBase>,
      IDiscoveryMethod
{
    /// <summary>
    /// Parameterless constructor for the Empty/NotFound sentinel (source-generated).
    /// </summary>
    protected DiscoveryMethodBase()
        : base(0, string.Empty)
    {
    }

    /// <summary>
    /// Constructor for concrete TypeOptions.
    /// </summary>
    protected DiscoveryMethodBase(
        int id,
        string name,
        string displayName,
        string description,
        bool supportsAutoDiscovery,
        IReadOnlyList<string> expectedProperties,
        IReadOnlyList<string> requiredProperties)
        : base(id, name, $"Discovery:{name}", displayName, description, "Discovery")
    {
        SupportsAutoDiscovery = supportsAutoDiscovery;
        ExpectedProperties = expectedProperties;
        RequiredProperties = requiredProperties;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> ExpectedProperties { get; } = [];

    /// <inheritdoc />
    public IReadOnlyList<string> RequiredProperties { get; } = [];

    /// <inheritdoc />
    public bool SupportsAutoDiscovery { get; }

    /// <inheritdoc />
    public bool IsEmpty => string.IsNullOrEmpty(Name);

    /// <inheritdoc />
    public abstract DiscoveryMethodBase CreateInstance();

    /// <summary>
    /// Binds this instance's settable properties from an IConfigurationSection.
    /// Base implementation is empty -- concrete types override to bind their own properties.
    /// </summary>
    public virtual void Bind(IConfigurationSection section)
    {
    }

    /// <summary>
    /// Decomposes this instance into key-value pairs for writing to the KVP table.
    /// Base implementation emits the Type discriminator.
    /// Each concrete type overrides to add its own properties.
    /// </summary>
    public virtual IReadOnlyList<KeyValuePair<string, string?>> AsKvp()
    {
        return [new("Type", Name)];
    }

    /// <summary>
    /// Hydrates this instance's settable properties from a key-value dictionary.
    /// Counterpart to <see cref="AsKvp"/>. Base implementation is empty --
    /// concrete types override to read their own properties.
    /// </summary>
    public virtual void BindFromValues(IReadOnlyDictionary<string, string?> values)
    {
    }

    /// <inheritdoc />
    public abstract IGenericResult Validate();
}
