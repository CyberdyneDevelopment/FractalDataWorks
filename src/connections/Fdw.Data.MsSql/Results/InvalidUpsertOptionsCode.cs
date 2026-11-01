using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.MsSql.Results;

/// <summary>Result code: the upsert options are inconsistent or name a column the container does not have.</summary>
[TypeOption(typeof(MsSqlDataResultCodes), "InvalidUpsertOptions", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InvalidUpsertOptionsCode : MsSqlDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidUpsertOptionsCode"/> class.
    /// </summary>
    public InvalidUpsertOptionsCode()
        : base(21006, "InvalidUpsertOptions",
            ResultSeverities.ByName("Error"),
            "Invalid upsert options: {Reason}",
            isRetryable: false)
    {
    }
}
