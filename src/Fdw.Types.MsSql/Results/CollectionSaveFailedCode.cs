using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Types.MsSql;

/// <summary>
/// Failed to save TypeCollection.
/// </summary>
[TypeOption(typeof(MsSqlTypesResultCodes), "CollectionSaveFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CollectionSaveFailedCode : MsSqlTypesResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CollectionSaveFailedCode"/> class.
    /// </summary>
    public CollectionSaveFailedCode()
        : base(70002, "CollectionSaveFailed",
            ResultSeverities.ByName("Error"),
            "Failed to save TypeCollection '{Name}': {ErrorMessage}",
            isRetryable: true)
    {
    }
}