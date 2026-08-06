using System;
using System.Threading.Tasks;

namespace Fdw.UI.CommandBuilders.Abstractions;

/// <summary>
/// Marker interface for any headless command-builder context.
/// Concrete contexts carry command-kind-specific state; this interface establishes
/// the contract that something is a headless command-builder state shape.
/// </summary>
/// <typeparam name="TCommandSpec">
/// The command specification type — the serializable shape that the builder
/// constructs and that the pipeline runtime deserializes at execution time.
/// </typeparam>
/// <remarks>
/// Why a separate interface for the context rather than just using the spec directly:
/// the context carries both the current spec AND the UI-level state (loading, errors,
/// available choices). Separating spec from context keeps serialized shapes lean while
/// allowing the builder to hold richer transient UI state.
/// </remarks>
public interface ICommandBuilderContext<TCommandSpec>
{
    /// <summary>Gets the current command specification being built.</summary>
    TCommandSpec CurrentSpec { get; }

    /// <summary>Gets the current command kind name (e.g., "Query", "Insert", "Update").</summary>
    string CurrentKind { get; }

    /// <summary>Gets a value indicating whether the provider is loading metadata.</summary>
    bool IsLoading { get; }

    /// <summary>Gets the current error message if loading failed; otherwise <c>null</c>.</summary>
    string? ErrorMessage { get; }

    /// <summary>
    /// Callback invoked by skins when the user changes the command spec.
    /// The provider serializes the updated spec and notifies the consuming page.
    /// </summary>
    Func<TCommandSpec, Task> OnChanged { get; }
}
