using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Abstractions.CommandCapabilities;

/// <summary>
/// The connection can sweep a staged key set (FDW-4956): a load appends each batch's keys to a stage table on this connection, and one soft delete at
/// the end flags the target rows with no staged key, checking first that the stage holds exactly the rows appended.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CommandCapabilityTypes), "KeyStage", RestrictToCurrentCompilation = true)]
public sealed class KeyStageCapability : CommandCapabilityTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KeyStageCapability"/> class.
    /// </summary>
    public KeyStageCapability()
        : base(
            name: "KeyStage",
            displayName: "Key stage (absent-key soft delete without holding every row)",
            configurationFields: [])
    {
    }
}
