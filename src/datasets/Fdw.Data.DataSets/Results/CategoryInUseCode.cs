using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataSets.Results;

/// <summary>
/// A category cannot be retired while categories sit under it or data sets sit under it: each would be left pointing at a node that is
/// gone. Conflict with the category's current state, HTTP 409 (Conflict category).
/// </summary>
[TypeOption(typeof(DataSetsResultCodes), "CategoryInUse", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CategoryInUseCode : DataSetsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="CategoryInUseCode"/> class.</summary>
    public CategoryInUseCode()
        : base(41102, "CategoryInUse",
            ResultSeverities.ByName("Error"),
            "Category '{category}' cannot be retired: {reason}",
            isRetryable: false)
    {
    }
}
