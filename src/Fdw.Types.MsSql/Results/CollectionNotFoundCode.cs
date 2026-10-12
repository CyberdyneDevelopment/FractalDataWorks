using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Types.MsSql;

/// <summary>
/// TypeCollection not found.
/// </summary>
[TypeOption(typeof(MsSqlTypesResultCodes), "CollectionNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CollectionNotFoundCode : MsSqlTypesResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CollectionNotFoundCode"/> class.
    /// </summary>
    public CollectionNotFoundCode()
        : base(30000, "CollectionNotFound",
            ResultSeverities.ByName("Error"),
            "TypeCollection '{Name}' was not found",
            isRetryable: false)
    {
    }
}