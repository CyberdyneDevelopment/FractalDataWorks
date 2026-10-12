using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.MsSql.Results;

/// <summary>Result code: the soft-delete options are inconsistent or name a column the container does not have.</summary>
[TypeOption(typeof(MsSqlDataResultCodes), "InvalidSoftDeleteOptions", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InvalidSoftDeleteOptionsCode : MsSqlDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidSoftDeleteOptionsCode"/> class.
    /// </summary>
    public InvalidSoftDeleteOptionsCode()
        : base(21007, "InvalidSoftDeleteOptions",
            ResultSeverities.ByName("Error"),
            "Invalid soft-delete options: {Reason}",
            isRetryable: false)
    {
    }
}
