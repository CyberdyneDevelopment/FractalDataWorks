using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Abstractions.CommandCapabilities;

/// <summary>
/// Upsert capability — inserts a row if no matching key exists, otherwise updates it.
/// The pipeline builder renders the <c>UpsertCommandSkin</c> composite component which
/// provides container picker, key-field selection, SET clause editor, and match condition builder.
/// </summary>
/// <remarks>
/// Why BuilderComponentType null: resolved at runtime by Builder.razor via
/// the capability's name; avoids a hard dependency from this netstandard2.0 package
/// on a Blazor UI package targeting net10.0.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CommandCapabilityTypes), "Upsert", RestrictToCurrentCompilation = true)]
public sealed class UpsertCapability : CommandCapabilityTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpsertCapability"/> class.
    /// </summary>
    public UpsertCapability()
        : base(
            id: 10,
            name: "Upsert",
            displayName: "Upsert (Insert or Update)",
            configurationFields: [],
            builderComponentType: null)
    {
    }
}
