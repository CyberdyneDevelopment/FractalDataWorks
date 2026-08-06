using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Abstractions.CommandCapabilities;

namespace Fdw.Services.Connections.RoslynWorkspace.Abstractions.CommandCapabilities;

/// <summary>
/// Get symbol source capability — retrieves source text for a Roslyn symbol by
/// its DocumentationCommentId.
/// Used by RoslynWorkspace connection types.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CommandCapabilityTypes), "GetSymbolSource", RestrictToCurrentCompilation = true)]
public sealed class GetSymbolSourceCapability : CommandCapabilityTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetSymbolSourceCapability"/> class.
    /// </summary>
    public GetSymbolSourceCapability()
        : base(
            id: 9,
            name: "GetSymbolSource",
            displayName: "Get Symbol Source",
            configurationFields:
            [
                new ConfigurationFieldDescriptor(
                    Key: "SymbolId",
                    Label: "Symbol ID",
                    Placeholder: "T:Foo.Bar or M:Foo.Bar.Baz(System.Int32)",
                    InputKind: ConfigurationFieldKinds.Text,
                    IsRequired: true),
            ])
    {
    }
}
