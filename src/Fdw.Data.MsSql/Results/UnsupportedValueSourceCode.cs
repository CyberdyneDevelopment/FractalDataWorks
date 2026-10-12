using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.MsSql.Results;

/// <summary>Result code: no renderer exists for a value source kind.</summary>
[TypeOption(typeof(MsSqlDataResultCodes), "UnsupportedValueSource", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class UnsupportedValueSourceCode : MsSqlDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnsupportedValueSourceCode"/> class.
    /// </summary>
    public UnsupportedValueSourceCode()
        : base(21010, "UnsupportedValueSource",
            ResultSeverities.ByName("Error"),
            "The value source '{Kind}' is not supported by this connection",
            isRetryable: false)
    {
    }
}
