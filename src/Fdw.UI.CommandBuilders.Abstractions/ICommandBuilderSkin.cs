using System;

namespace Fdw.UI.CommandBuilders.Abstractions;

/// <summary>
/// Descriptor interface implemented by Blazor components that act as visual skins
/// for a specific command kind inside a <see cref="ICommandBuilderProvider{TCommandSpec}"/>.
/// </summary>
/// <remarks>
/// Each skin declares which command kind it handles (<see cref="SupportedKind"/>) and
/// which context type it consumes (<see cref="ContextType"/>). Builder.razor uses this
/// information when wiring up <c>DynamicComponent</c> parameters.
///
/// Why a runtime interface rather than an attribute: Blazor's <c>DynamicComponent</c>
/// already uses a <c>Type</c> reference to instantiate the component. Adding an interface
/// lets Builder.razor verify the type contract with a cast check rather than reflection,
/// producing a clear warning when a misconfigured capability points to a non-skin type.
/// </remarks>
public interface ICommandBuilderSkin
{
    /// <summary>
    /// Gets the command kind name this skin handles (e.g., "Query", "Insert", "Update").
    /// Must match <see cref="ICommandBuilderContext{TCommandSpec}.CurrentKind"/> of the context
    /// passed to this skin.
    /// </summary>
    string SupportedKind { get; }

    /// <summary>
    /// Gets the concrete <see cref="ICommandBuilderContext{TCommandSpec}"/> type this skin
    /// expects as its primary parameter.
    /// Builder.razor uses this to verify that the provider and skin share a compatible context type.
    /// </summary>
    Type ContextType { get; }
}
