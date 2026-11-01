using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Workspaces.Results;

/// <summary>A data view cannot be deleted while other views are built on it.</summary>
/// <remarks>Category 4 (Conflict). A view of a view reads through its parent, so deleting the parent would leave the child with nothing to read (FDW-1032). The reason names the first dependent view. HTTP 409.</remarks>
[TypeOption(typeof(WorkspacesResultCodes), "DataViewHasDependents", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DataViewHasDependentsCode : WorkspacesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DataViewHasDependentsCode"/> class.</summary>
    public DataViewHasDependentsCode()
        : base(41000, "DataViewHasDependents",
            ResultSeverities.ByName("Error"),
            "Data view '{name}' cannot be deleted: {reason}",
            isRetryable: false)
    {
    }
}
