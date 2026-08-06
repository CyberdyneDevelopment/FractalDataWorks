using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// Base class for command capability type options.
/// Derive and annotate with <c>[TypeOption(typeof(CommandCapabilityTypes), "Name")]</c>.
/// </summary>
/// <remarks>
/// Why CRTP with int key: capabilities use sequential integer IDs (same as ContainerWriteModeBase).
/// The ID is only used internally by the TypeCollection for O(1) lookup — it never leaves the process.
/// </remarks>
[ExcludeFromCodeCoverage]
public abstract class CommandCapabilityTypeBase : TypeOptionBase<int, CommandCapabilityTypeBase>, ICommandCapabilityType
{
    /// <summary>
    /// Required protected parameterless constructor for the TypeCollection Empty sentinel.
    /// Should not be used in application code.
    /// </summary>
    protected CommandCapabilityTypeBase()
        : base(0, "NotFound")
    {
        ConfigurationFields = [];
        BuilderComponentType = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandCapabilityTypeBase"/> class.
    /// </summary>
    /// <param name="id">Unique integer ID within the TypeCollection (sequential, 1-based).</param>
    /// <param name="name">The TypeCollection lookup name (e.g., <c>"RawQuery"</c>).</param>
    /// <param name="displayName">Human-readable label shown in the capability picker dropdown.</param>
    /// <param name="configurationFields">
    /// Field descriptors rendered as the properties-panel form for this capability.
    /// Pass an empty list when a <paramref name="builderComponentType"/> handles all rendering.
    /// </param>
    /// <param name="builderComponentType">
    /// Optional Blazor component type that renders a composite widget.
    /// When non-null the builder renders this component instead of <paramref name="configurationFields"/>.
    /// </param>
    protected CommandCapabilityTypeBase(
        int id,
        string name,
        string displayName,
        IReadOnlyList<ConfigurationFieldDescriptor> configurationFields,
        Type? builderComponentType = null)
        : base(id, name, $"CommandCapabilities:{name}", displayName, $"Command capability: {name}", "CommandCapability")
    {
        ConfigurationFields = configurationFields;
        BuilderComponentType = builderComponentType;
    }

    /// <inheritdoc/>
    public IReadOnlyList<ConfigurationFieldDescriptor> ConfigurationFields { get; }

    /// <inheritdoc/>
    public Type? BuilderComponentType { get; }
}
