using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataSets.Results;

/// <summary>
/// A category in a create, update or read was refused: a name or display name left out, a parent that is not a category, a
/// move that would make a category sit under itself, or a stored tree that is not sound. Caller-input validation failure: HTTP 400
/// (Validation category).
/// </summary>
[TypeOption(typeof(DataSetsResultCodes), "CategoryInvalid", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CategoryInvalidCode : DataSetsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="CategoryInvalidCode"/> class.</summary>
    public CategoryInvalidCode()
        : base(20016, "CategoryInvalid",
            ResultSeverities.ByName("Error"),
            "The category is refused: {reason}",
            isRetryable: false)
    {
    }
}
