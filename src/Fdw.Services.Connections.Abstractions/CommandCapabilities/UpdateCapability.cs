using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Abstractions.CommandCapabilities;

/// <summary>
/// Update capability — modifies existing rows in a target container matching a WHERE filter.
/// ConfigurationFields is empty: the client edits this with its own editor (container picker, SET clause editor, and filter builder).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CommandCapabilityTypes), "Update", RestrictToCurrentCompilation = true)]
public sealed class UpdateCapability : CommandCapabilityTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateCapability"/> class.
    /// </summary>
    public UpdateCapability()
        : base(
            name: "Update",
            displayName: "Update",
            configurationFields: [])
    {
    }
}
