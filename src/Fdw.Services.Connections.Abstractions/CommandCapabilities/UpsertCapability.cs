using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Abstractions.CommandCapabilities;

/// <summary>
/// Upsert capability — inserts a row if no matching key exists, otherwise updates it.
/// ConfigurationFields is empty: the client edits this with its own editor (container picker, key-field selection, SET clause editor, and match condition builder).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CommandCapabilityTypes), "Upsert", RestrictToCurrentCompilation = true)]
public sealed class UpsertCapability : CommandCapabilityTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpsertCapability"/> class.
    /// </summary>
    public UpsertCapability()
        : base(
            name: "Upsert",
            displayName: "Upsert (Insert or Update)",
            configurationFields: [])
    {
    }
}
