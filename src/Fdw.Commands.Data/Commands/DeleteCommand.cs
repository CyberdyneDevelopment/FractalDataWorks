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
/// Delete command for removing records (DELETE operation).
/// Returns the number of affected rows.
/// </summary>
/// <remarks>
/// <para>
/// This command represents a universal DELETE operation that works across all data sources.
/// Translators convert it to:
/// <list type="bullet">
/// <item>SQL: DELETE statement with WHERE clause</item>
/// <item>REST: DELETE request</item>
/// <item>File: Remove records</item>
/// <item>GraphQL: deleteX mutation</item>
/// </list>
/// </para>
/// <para>
/// Example usage:
/// <code>
/// var call = DataDelete.From("CRM", "sales", "Customers")
///     .Where("IsActive", false)
///     .Build();
/// var result = await gateway.Execute&lt;int&gt;(call, ct);
/// </code>
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataCommands), "Delete", RestrictToCurrentCompilation = true)]
public sealed class DeleteCommand : DataCommandBase<int>, IFilterableCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteCommand"/> class.
    /// </summary>
    public DeleteCommand()
        : base("Delete")
    {
        DeleteCommandLog.CommandCreated(NullLogger<DeleteCommand>.Instance);
    }

    /// <summary>
    /// Gets or sets the filter expression (WHERE clause for delete).
    /// Determines which records to delete.
    /// </summary>
    public IFilterExpression? Filter { get; init; }
}
