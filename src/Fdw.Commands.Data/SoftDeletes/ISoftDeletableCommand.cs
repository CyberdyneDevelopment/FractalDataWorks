using Fdw.Commands.Data.Abstractions;

namespace Fdw.Commands.Data.SoftDeletes;

/// <summary>
/// A command that can flag rows deleted instead of removing them: <see cref="DeleteCommand"/> and
/// <see cref="UpdateCommand{T}"/>. When <see cref="SoftDelete"/> is set the translator sets the configured
/// deleted-flag column rather than removing or only updating the row.
/// </summary>
public interface ISoftDeletableCommand : IDataCommand
{
    /// <summary>Gets the soft-delete options, or <see langword="null"/> for the command's ordinary behaviour.</summary>
    SoftDeleteOptions? SoftDelete { get; }
}
