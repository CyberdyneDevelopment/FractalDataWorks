using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Commands.Abstractions;
using Fdw.Commands.Data.Abstractions;

namespace Fdw.Commands.Data;

/// <summary>
/// Insert command for adding new records (INSERT operation).
/// Returns the number of affected rows or identity value.
/// </summary>
/// <typeparam name="T">The type of entity to insert.</typeparam>
/// <remarks>
/// <para>
/// This command represents a universal INSERT operation that works across all data sources.
/// Translators convert it to:
/// <list type="bullet">
/// <item>SQL: INSERT INTO statement</item>
/// <item>REST: POST request</item>
/// <item>File: Append record</item>
/// <item>GraphQL: createX mutation</item>
/// </list>
/// </para>
/// <para>
/// Example usage:
/// <code>
/// var customer = new Customer { Name = "Acme Corp", IsActive = true };
/// var call = DataInsert.Into&lt;Customer&gt;("CRM", "sales", "Customers").Value(customer);
/// var result = await gateway.Execute&lt;int&gt;(call, ct);
/// </code>
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataCommands), "Insert", RestrictToCurrentCompilation = true)]
public sealed class InsertCommand<T> : DataCommandBase<int, T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InsertCommand{T}"/> class.
    /// </summary>
    /// <param name="data">The entity to insert.</param>
    public InsertCommand(T data)
        : base("Insert", data)
    {
    }
}
