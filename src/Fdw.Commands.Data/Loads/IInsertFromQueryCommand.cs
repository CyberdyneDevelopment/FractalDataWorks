using Fdw.Commands.Data.Abstractions;

namespace Fdw.Commands.Data.Loads;

/// <summary>
/// A set-based INSERT ... SELECT on one connection. A translator reads the write columns, value sources and source from
/// <see cref="Options"/>.
/// </summary>
public interface IInsertFromQueryCommand : IDataCommand
{
    /// <summary>Gets the write columns, the value sources, and the source rows.</summary>
    InsertFromQueryOptions Options { get; }
}
