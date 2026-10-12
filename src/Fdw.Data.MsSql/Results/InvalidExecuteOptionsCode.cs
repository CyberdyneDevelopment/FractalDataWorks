using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.MsSql.Results;

/// <summary>Result code: the container is not executable here, or the execute parameters cannot be used against it.</summary>
[TypeOption(typeof(MsSqlDataResultCodes), "InvalidExecuteOptions", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InvalidExecuteOptionsCode : MsSqlDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidExecuteOptionsCode"/> class.
    /// </summary>
    public InvalidExecuteOptionsCode()
        : base(21011, "InvalidExecuteOptions",
            ResultSeverities.ByName("Error"),
            "Invalid execute options: {Reason}",
            isRetryable: false)
    {
    }
}
