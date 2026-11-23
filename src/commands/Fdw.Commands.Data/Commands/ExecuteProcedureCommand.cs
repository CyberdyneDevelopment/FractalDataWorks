using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;

namespace Fdw.Commands.Data;

/// <summary>
/// Runs the stored procedure a <c>StoredProcedure</c> container names and answers the rows it returns as <typeparamref name="T"/> (FDW-6682).
/// </summary>
/// <typeparam name="T">The row type the procedure returns.</typeparam>
/// <remarks>
/// <para>
/// Why a command and not SQL text: every parameter is carried as a bound value, so a value is never part of the statement. The container states the operation <c>Execute</c>
/// and no other, and the shared operation rule refuses any other command against it. A procedure runs with no row rules of the caller's: no HTTP route issues this
/// command, and a procedure that touches tenant rows takes the tenant as a parameter.
/// </para>
/// <para>
/// The rows are those of EVERY result set the procedure answers, in the order it answers them (FDW-7403), whatever the connection type: each set has its own columns and the sets are
/// not marked apart, so a caller that needs to know which set a row came from tells them by their columns (a lapsed claim has a <c>TenantId</c>, a renewal a <c>Renewed</c>). A procedure
/// whose two sets have the same columns is ambiguous for such a caller. A connection that reads only the first set loses the others, and the procedure has already done what it did.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataCommands), "Execute", RestrictToCurrentCompilation = true)]
public sealed class ExecuteProcedureCommand<T> : DataCommandBase<IEnumerable<T>>, IExecuteProcedureCommand
{
    /// <summary>Initializes a new instance of the <see cref="ExecuteProcedureCommand{T}"/> class.</summary>
    public ExecuteProcedureCommand()
        : base("Execute")
    {
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?> Parameters { get; init; } = new Dictionary<string, object?>(System.StringComparer.Ordinal);
}
