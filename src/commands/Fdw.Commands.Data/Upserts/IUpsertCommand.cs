using Fdw.Commands.Data.Abstractions;

namespace Fdw.Commands.Data.Upserts;

/// <summary>
/// A command that writes source rows into a target by matching on key columns: <see cref="UpsertCommand{T}"/>. A
/// translator reads the rows from <see cref="IDataCommandWithInput.InputData"/> and the behaviour from
/// <see cref="Options"/>.
/// </summary>
public interface IUpsertCommand : IDataCommandWithInput
{
    /// <summary>Gets the key, compare and write columns and the optional versioning and insert-only behaviour.</summary>
    UpsertOptions Options { get; }
}
