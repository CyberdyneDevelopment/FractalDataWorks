using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Data.Results;

/// <summary>
/// DataPath was null.
/// </summary>
[TypeOption(typeof(DataServiceResultCodes), "DataPathRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DataPathRequiredCode : DataServiceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataPathRequiredCode"/> class.
    /// </summary>
    public DataPathRequiredCode()
        : base(21001, "DataPathRequired", ResultSeverities.ByName("Error"),
            "DataPath cannot be null",
            isRetryable: false)
    {
    }
}