using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Commands.Abstractions;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Logging;
using Fdw.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Commands.Data;

/// <summary>
/// Update command for modifying existing records (UPDATE operation).
/// Returns the number of affected rows.
/// </summary>
/// <typeparam name="T">The type of entity to update.</typeparam>
/// <remarks>
/// <para>
/// This command represents a universal UPDATE operation that works across all data sources.
/// Translators convert it to:
/// <list type="bullet">
/// <item>SQL: UPDATE statement with WHERE clause</item>
/// <item>REST: PUT/PATCH request</item>
/// <item>File: Update record</item>
/// <item>GraphQL: updateX mutation</item>
/// </list>
/// </para>
/// <para>
/// Example usage:
/// <code>
/// var call = DataUpdate.In&lt;Customer&gt;("CRM", "sales", "Customers")
///     .Where(c => c.Id).Equal(123)
///     .Value(updatedCustomer);
/// var result = await gateway.Execute&lt;int&gt;(call, ct);
/// </code>
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataCommands), "Update", RestrictToCurrentCompilation = true)]
public sealed class UpdateCommand<T> : DataCommandBase<int, T>, IFilterableCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateCommand{T}"/> class.
    /// </summary>
    /// <param name="data">The updated entity data.</param>
    public UpdateCommand(T data)
        : base("Update", data)
    {
        UpdateCommandLog.CommandCreated(NullLogger<UpdateCommand<T>>.Instance, typeof(T).Name);
    }

    /// <summary>
    /// Gets or sets the filter expression (WHERE clause for update).
    /// Determines which records to update.
    /// </summary>
    public IFilterExpression? Filter { get; init; }
}
