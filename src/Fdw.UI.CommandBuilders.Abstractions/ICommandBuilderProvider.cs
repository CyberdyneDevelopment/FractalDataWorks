using System.Threading;
using System.Threading.Tasks;

namespace Fdw.UI.CommandBuilders.Abstractions;

/// <summary>
/// Marker interface for the headless provider Blazor component of a command builder.
/// Implementations expose <c>ChildContent: RenderFragment&lt;ICommandBuilderContext&lt;TCommandSpec&gt;&gt;</c>
/// and the state-management callbacks their concrete context type needs.
/// </summary>
/// <typeparam name="TCommandSpec">
/// The command specification type produced by this provider.
/// </typeparam>
/// <remarks>
/// Why a separate interface from the Blazor component class: Blazor components inherit
/// <c>ComponentBase</c> and cannot implement multiple base classes. This interface
/// provides a discoverable contract without constraining the base class hierarchy.
/// </remarks>
public interface ICommandBuilderProvider<TCommandSpec>
{
    /// <summary>
    /// Validates the current command spec and returns a result indicating whether
    /// it is well-formed enough to be persisted.
    /// </summary>
    Task<bool> Validate(CancellationToken cancellationToken = default);

    /// <summary>
    /// Serializes the current command spec to a JSON string suitable for storage
    /// in the pipeline task's Configuration dictionary under the key "CommandSpec".
    /// </summary>
    Task<string> Serialize(CancellationToken cancellationToken = default);
}
