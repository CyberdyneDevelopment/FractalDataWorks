using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Abstractions.CommandCapabilities;

/// <summary>
/// Scripted capability: the store takes text a person wrote (SQL for a SQL database) and runs it with named parameters, as a
/// query that answers rows or as a command that answers how many rows it affected (FDW-1435). It replaces the earlier raw
/// query capability, which no connection implemented.
/// </summary>
/// <remarks>
/// No shared connection type lists it: scripts are an ETL-only extension (FDW-1447), found through
/// <see cref="ScriptedConnectionTypes"/>. Configuration key: <c>Text</c>, the script.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CommandCapabilityTypes), "Scripted", RestrictToCurrentCompilation = true)]
public sealed class ScriptedCapability : CommandCapabilityTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptedCapability"/> class.
    /// </summary>
    public ScriptedCapability()
        : base(
            name: "Scripted",
            displayName: "Scripts (queries and commands)",
            configurationFields:
            [
                new ConfigurationFieldDescriptor(
                    Key: "Text",
                    Label: "Script",
                    Placeholder: "Enter a query or a command (SQL for a SQL database)",
                    InputKind: ConfigurationFieldKinds.Textarea,
                    IsRequired: true),
            ])
    {
    }
}
