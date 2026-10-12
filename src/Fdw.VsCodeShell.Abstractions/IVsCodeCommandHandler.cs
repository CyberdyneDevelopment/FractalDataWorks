using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.VsCodeShell.Abstractions;

/// <summary>
/// The behaviour behind a VS Code command. Pure behaviour — the command's identity and metadata live on
/// its <see cref="VsCodeCommandTypeBase{THandler}"/> option, which is the single place they are declared.
/// </summary>
/// <remarks>
/// Implementations are registered keyed on the owning option's <see cref="IVsCodeCommandType.CommandId"/>
/// and resolved by key, so a handler no longer carries — or can disagree with — its own descriptor.
/// </remarks>
public interface IVsCodeCommandHandler
{
    /// <summary>Invoked when the user fires the command in VS Code.</summary>
    Task<IGenericResult<object?>> Invoke(EditorContext context, CancellationToken cancellationToken = default);
}
