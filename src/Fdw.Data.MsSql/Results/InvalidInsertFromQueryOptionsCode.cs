using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.MsSql.Results;

/// <summary>Result code: the insert-from-query options are inconsistent or name a column the container does not have.</summary>
[TypeOption(typeof(MsSqlDataResultCodes), "InvalidInsertFromQueryOptions", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InvalidInsertFromQueryOptionsCode : MsSqlDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidInsertFromQueryOptionsCode"/> class.
    /// </summary>
    public InvalidInsertFromQueryOptionsCode()
        : base(21008, "InvalidInsertFromQueryOptions",
            ResultSeverities.ByName("Error"),
            "Invalid insert-from-query options: {Reason}",
            isRetryable: false)
    {
    }
}
