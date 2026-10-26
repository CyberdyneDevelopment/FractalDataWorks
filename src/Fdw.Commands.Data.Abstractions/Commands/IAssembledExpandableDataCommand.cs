using System.Collections.Generic;
using Fdw.Results;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// An expandable command that answers something other than a row count: its own result, put together from what each
/// of the steps it expands into answered.
/// </summary>
/// <typeparam name="TResult">The result the command answers.</typeparam>
/// <remarks>
/// A connection runs the steps in order and, when it can open a transaction, in one, so either every step is done or
/// none is. A step that answers rows is marked in its <see cref="ExpandedCommand"/>; the others answer a count.
/// </remarks>
public interface IAssembledExpandableDataCommand<TResult> : IExpandableDataCommand
{
    /// <summary>Puts the command's result together from what each step answered.</summary>
    /// <param name="answers">One answer for each step, in the order the steps ran.</param>
    /// <returns>The command's result; or why the answers cannot make one.</returns>
    IGenericResult<TResult> Assemble(IReadOnlyList<ExpansionAnswer> answers);
}
