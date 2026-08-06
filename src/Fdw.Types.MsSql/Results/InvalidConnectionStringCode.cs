using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Types.MsSql;

/// <summary>
/// Invalid connection string provided.
/// </summary>
[TypeOption(typeof(MsSqlTypesResultCodes), "InvalidConnectionString", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InvalidConnectionStringCode : MsSqlTypesResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidConnectionStringCode"/> class.
    /// </summary>
    public InvalidConnectionStringCode()
        : base(61000, "InvalidConnectionString",
            ResultSeverities.ByName("Error"),
            "Connection string is null or empty",
            isRetryable: false)
    {
    }
}