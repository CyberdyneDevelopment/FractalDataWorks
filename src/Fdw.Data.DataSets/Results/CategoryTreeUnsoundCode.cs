using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataSets.Results;

/// <summary>
/// The stored categories do not make a tree: a blank field, a name or identity used twice, a parent that is not a category, or a
/// cycle. That is a fault in the store, not in the request, so it is not a validation failure (HTTP 500, Configuration category).
/// </summary>
[TypeOption(typeof(DataSetsResultCodes), "CategoryTreeUnsound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CategoryTreeUnsoundCode : DataSetsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="CategoryTreeUnsoundCode"/> class.</summary>
    public CategoryTreeUnsoundCode()
        : base(60002, "CategoryTreeUnsound",
            ResultSeverities.ByName("Error"),
            "The stored categories do not make a tree: {reason}",
            isRetryable: false)
    {
    }
}
