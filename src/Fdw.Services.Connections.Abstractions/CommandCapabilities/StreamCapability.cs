using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Abstractions.CommandCapabilities;

/// <summary>
/// Stream capability — reads a continuous or resumable data stream from the connection.
/// Supports optional start-offset (e.g., Kafka partition offset, change-feed cursor)
/// and max-rows limit for bounded streaming in test mode.
/// </summary>
/// <remarks>
/// Configuration keys:
/// <list type="bullet">
///   <item><c>StartOffset</c> — resume token or start position (optional; connection-specific format).</item>
///   <item><c>MaxRows</c> — maximum records to read before stopping; 0 means unlimited.</item>
/// </list>
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CommandCapabilityTypes), "Stream", RestrictToCurrentCompilation = true)]
public sealed class StreamCapability : CommandCapabilityTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StreamCapability"/> class.
    /// </summary>
    public StreamCapability()
        : base(
            id: 5,
            name: "Stream",
            displayName: "Stream",
            configurationFields:
            [
                new ConfigurationFieldDescriptor(
                    Key: "StartOffset",
                    Label: "Start Offset",
                    Placeholder: "Leave blank to start from the beginning",
                    InputKind: ConfigurationFieldKinds.Text),
                new ConfigurationFieldDescriptor(
                    Key: "MaxRows",
                    Label: "Max Rows (0 = unlimited)",
                    Placeholder: "0",
                    InputKind: ConfigurationFieldKinds.Numeric),
            ])
    {
    }
}
