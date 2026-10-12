using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Commands.Abstractions;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Commands.Data;

/// <summary>
/// Find command for cross-field text search across a container's string fields.
/// Returns matched records along with which fields contained the search term.
/// </summary>
/// <typeparam name="T">The type of entity to search.</typeparam>
/// <remarks>
/// <para>
/// This command represents a universal cross-field search operation that works across all data sources.
/// Translators convert it to:
/// <list type="bullet">
/// <item>SQL: SELECT with LIKE/CONTAINS across specified columns</item>
/// <item>REST: Full-text search endpoint</item>
/// <item>File: Scan and match string fields</item>
/// </list>
/// </para>
/// <para>
/// Example usage:
/// <code>
/// var buildResult = Find.In&lt;Customer&gt;("CRM", "sales", "Customers")
///     .Search("acme")
///     .InFields("Name", "Description", "Email")
///     .CaseSensitive(false)
///     .MaxResults(50)
///     .Build();
/// if (!buildResult.IsSuccess) return buildResult.ToNewResult&lt;...&gt;();
/// var result = await gateway.Execute&lt;IEnumerable&lt;FindResult&lt;Customer&gt;&gt;&gt;(buildResult.Value, ct);
/// </code>
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataCommands), "Find", RestrictToCurrentCompilation = true)]
public sealed class FindCommand<T> : DataCommandBase<IEnumerable<FindResult<T>>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FindCommand{T}"/> class.
    /// </summary>
    public FindCommand()
        : base("Find")
    {
        FindCommandLog.CommandCreated(NullLogger<FindCommand<T>>.Instance, typeof(T).Name);
    }

    /// <summary>
    /// Gets the search term to find across fields.
    /// </summary>
    public string SearchTerm { get; init; } = string.Empty;

    /// <summary>
    /// Gets the field names to search within.
    /// When null or empty, all string fields are searched.
    /// </summary>
    public IReadOnlyList<string>? FieldNames { get; init; }

    /// <summary>
    /// Gets a value indicating whether the search is case-sensitive.
    /// Defaults to false (case-insensitive).
    /// </summary>
    public bool CaseSensitive { get; init; }

    /// <summary>
    /// Gets the maximum number of results to return.
    /// When null, the system default limit is applied.
    /// </summary>
    public int? MaxResults { get; init; }
}
