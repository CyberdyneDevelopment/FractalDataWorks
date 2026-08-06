using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Abstractions.CommandCapabilities;

/// <summary>
/// Update capability — modifies existing rows in a target container matching a WHERE filter.
/// The pipeline builder renders the <c>UpdateCommandSkin</c> composite component which
/// provides container picker, SET clause editor, and filter builder.
/// </summary>
/// <remarks>
/// Why BuilderComponentType null: resolved at runtime by Builder.razor via
/// the capability's name; avoids a hard dependency from this netstandard2.0 package
/// on a Blazor UI package targeting net10.0.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CommandCapabilityTypes), "Update", RestrictToCurrentCompilation = true)]
public sealed class UpdateCapability : CommandCapabilityTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateCapability"/> class.
    /// </summary>
    public UpdateCapability()
        : base(
            id: 9,
            name: "Update",
            displayName: "Update",
            configurationFields: [],
            builderComponentType: null)
    {
    }
}
