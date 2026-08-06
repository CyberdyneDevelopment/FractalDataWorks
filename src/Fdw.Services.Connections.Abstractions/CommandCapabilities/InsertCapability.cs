using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Abstractions.CommandCapabilities;

/// <summary>
/// Insert capability — writes a single row or a small set of rows to a target container.
/// The pipeline builder renders the <c>InsertCommandSkin</c> composite component which
/// provides container picker, field-picker, and per-field value entry.
/// </summary>
/// <remarks>
/// Why BuilderComponentType null: resolved at runtime by Builder.razor via
/// the capability's name; avoids a hard dependency from this netstandard2.0 package
/// on a Blazor UI package targeting net10.0.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CommandCapabilityTypes), "Insert", RestrictToCurrentCompilation = true)]
public sealed class InsertCapability : CommandCapabilityTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InsertCapability"/> class.
    /// </summary>
    public InsertCapability()
        : base(
            id: 8,
            name: "Insert",
            displayName: "Insert",
            configurationFields: [],
            builderComponentType: null)
    {
    }
}
