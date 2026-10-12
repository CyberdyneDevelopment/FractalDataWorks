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
public abstract class CommandCapabilityTypeBase : TypeOptionBase<CommandCapabilityTypeBase>, ICommandCapabilityType
{
    /// <summary>
    /// Required protected parameterless constructor for the TypeCollection Empty sentinel.
    /// Should not be used in application code.
    /// </summary>
    protected CommandCapabilityTypeBase()
        : base(0, "NotFound")
    {
        ConfigurationFields = [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandCapabilityTypeBase"/> class.
    /// </summary>
    /// <param name="name">The TypeCollection lookup name (e.g., <c>"RawQuery"</c>).</param>
    /// <param name="displayName">Human-readable label shown in the capability picker dropdown.</param>
    /// <param name="configurationFields">
    /// Field descriptors rendered as the properties-panel form for this capability.
    /// Pass an empty list when the client edits this capability's configuration with its own editor.
    /// </param>
    protected CommandCapabilityTypeBase(
        string name,
        string displayName,
        IReadOnlyList<ConfigurationFieldDescriptor> configurationFields)
        : base(name, $"CommandCapabilities:{name}", displayName, $"Command capability: {name}", "CommandCapability")
    {
        ConfigurationFields = configurationFields;
    }

    /// <inheritdoc/>
    public IReadOnlyList<ConfigurationFieldDescriptor> ConfigurationFields { get; }

}
