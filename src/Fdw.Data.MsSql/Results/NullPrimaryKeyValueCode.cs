using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.MsSql.Results;

/// <summary>
/// Primary key value is null; UPDATE cannot proceed without a valid primary key.
/// </summary>
[TypeOption(typeof(MsSqlDataResultCodes), "NullPrimaryKeyValue", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NullPrimaryKeyValueCode : MsSqlDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NullPrimaryKeyValueCode"/> class.
    /// </summary>
    public NullPrimaryKeyValueCode()
        : base(21004, "NullPrimaryKeyValue",
            ResultSeverities.ByName("Error"),
            "Primary key '{PrimaryKeyField}' has a null value; UPDATE requires a non-null primary key",
            isRetryable: false)
    {
    }
}
