using System.Collections;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// A command that writes rows it carries (FDW-5069): what lets a caller check each row before the command is sent.
/// </summary>
public interface IRowInputCommand : IDataCommandWithInput
{
    /// <summary>Gets the rows the command writes, in order.</summary>
    IEnumerable InputRows { get; }
}
