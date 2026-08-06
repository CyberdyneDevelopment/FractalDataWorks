using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Abstractions.CommandCapabilities;

/// <summary>
/// Execute capability — invokes a named stored procedure, function, or API endpoint.
/// Renders a <c>Name</c> text field and a <c>Parameters</c> key-value list in the builder.
/// </summary>
/// <remarks>
/// Configuration keys:
/// <list type="bullet">
///   <item><c>Name</c> — the stored procedure or endpoint name (required).</item>
///   <item><c>Parameters</c> — JSON array of <c>{"Key":"…","Value":"…"}</c> parameter entries.</item>
/// </list>
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CommandCapabilityTypes), "Execute", RestrictToCurrentCompilation = true)]
public sealed class ExecuteCapability : CommandCapabilityTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExecuteCapability"/> class.
    /// </summary>
    public ExecuteCapability()
        : base(
            id: 3,
            name: "Execute",
            displayName: "Execute",
            configurationFields:
            [
                new ConfigurationFieldDescriptor(
                    Key: "Name",
                    Label: "Procedure / Endpoint Name",
                    Placeholder: "schema.ProcedureName",
                    InputKind: ConfigurationFieldKinds.Text,
                    IsRequired: true),
                new ConfigurationFieldDescriptor(
                    Key: "Parameters",
                    Label: "Parameters",
                    Placeholder: "Key-value parameter pairs",
                    InputKind: ConfigurationFieldKinds.KeyValueList),
            ])
    {
    }
}
