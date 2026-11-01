using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Types.MsSql;

/// <summary>
/// DDL resource not found.
/// </summary>
[TypeOption(typeof(MsSqlTypesResultCodes), "DdlResourceNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DdlResourceNotFoundCode : MsSqlTypesResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DdlResourceNotFoundCode"/> class.
    /// </summary>
    public DdlResourceNotFoundCode()
        : base(31000, "DdlResourceNotFound",
            ResultSeverities.ByName("Error"),
            "Embedded DDL resource not found in assembly",
            isRetryable: false)
    {
    }
}