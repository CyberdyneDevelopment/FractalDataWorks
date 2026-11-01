using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data.DataSets.Abstractions;

namespace Fdw.Services.Workspaces;

/// <summary>
/// A data set a caller reaches through a workspace: the name it is attached under, and the data set a
/// query of it actually runs.
/// </summary>
/// <remarks>
/// The two differ for a variation. A variation holds no fields or sources of its own, so the name the
/// caller asks for is the variation's while the fields read and the query run are its main data set's,
/// under the access the workspace gave. A view of a data set also narrows the rows: its stored filter, and the
/// stored filter of every view above it, ride along in <see cref="ViewFilters"/> for the query to apply (FDW-1032).
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class ReadableDataSet
{
    /// <summary>Initializes a new instance of the <see cref="ReadableDataSet"/> class.</summary>
    /// <param name="name">The name the data set is attached to the workspace under.</param>
    /// <param name="executionTarget">The data set whose fields are read and whose query runs.</param>
    /// <param name="viewFilters">
    /// The stored filters that narrow the rows, parent view first; empty for a data set attached to the workspace,
    /// which no view narrows.
    /// </param>
    public ReadableDataSet(string name, IDataSetImplementationConfiguration executionTarget, IReadOnlyList<string> viewFilters)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        ExecutionTarget = executionTarget ?? throw new ArgumentNullException(nameof(executionTarget));
        ViewFilters = viewFilters ?? throw new ArgumentNullException(nameof(viewFilters));
    }

    /// <summary>Gets the name the data set is attached to the workspace under.</summary>
    public string Name { get; }

    /// <summary>Gets the data set whose fields are read and whose query runs.</summary>
    public IDataSetImplementationConfiguration ExecutionTarget { get; }

    /// <summary>
    /// Gets the stored filters that narrow the rows, parent view first, each a filter tree as JSON. Empty when no view
    /// narrows this data set, which is true of an attached data set and of a view none of whose chain stores a filter.
    /// </summary>
    public IReadOnlyList<string> ViewFilters { get; }
}
