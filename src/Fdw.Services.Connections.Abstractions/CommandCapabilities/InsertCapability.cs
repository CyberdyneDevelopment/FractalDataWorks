using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Abstractions.CommandCapabilities;

/// <summary>
/// Insert capability — writes a single row or a small set of rows to a target container.
/// ConfigurationFields is empty: the client edits this with its own editor (container picker, field-picker, and per-field value entry).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CommandCapabilityTypes), "Insert", RestrictToCurrentCompilation = true)]
public sealed class InsertCapability : CommandCapabilityTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InsertCapability"/> class.
    /// </summary>
    public InsertCapability()
        : base(
            name: "Insert",
            displayName: "Insert",
            configurationFields: [])
    {
    }
}
