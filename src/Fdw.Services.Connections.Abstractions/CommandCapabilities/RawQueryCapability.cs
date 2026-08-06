using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Abstractions.CommandCapabilities;

/// <summary>
/// Raw query capability — sends a verbatim text query (SQL, KQL, SOQL, etc.) to the connection.
/// Renders a single <c>Query</c> textarea field in the pipeline builder properties panel.
/// </summary>
/// <remarks>
/// Configuration key: <c>Query</c> — the raw query text.
/// Connection types that support both <see cref="QueryCapability"/> and RawQueryCapability
/// expose both in their <c>SupportedCommands</c> list.
/// Tasks saved before FDW-391 that have a <c>Query</c> key but no <c>CapabilityName</c>
/// are treated as RawQuery by the builder (backward-compatible safety heuristic).
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CommandCapabilityTypes), "RawQuery", RestrictToCurrentCompilation = true)]
public sealed class RawQueryCapability : CommandCapabilityTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RawQueryCapability"/> class.
    /// </summary>
    public RawQueryCapability()
        : base(
            id: 2,
            name: "RawQuery",
            displayName: "Raw Query",
            configurationFields:
            [
                new ConfigurationFieldDescriptor(
                    Key: "Query",
                    Label: "Query",
                    Placeholder: "Enter your query (SQL, KQL, etc.)",
                    InputKind: ConfigurationFieldKinds.Textarea,
                    IsRequired: true),
            ])
    {
    }
}
